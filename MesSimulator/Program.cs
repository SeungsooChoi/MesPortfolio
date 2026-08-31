using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

// -------------------------------------------------
// 설정 읽기
// -------------------------------------------------
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

string connStr = config.GetConnectionString("Mes")
    ?? throw new Exception("appsettings.json 에 ConnectionStrings:Mes 가 없습니다.");

double speed = double.Parse(config["Simulator:Speed"] ?? "1");
double hoursBack = double.Parse(config["Simulator:HoursBack"] ?? "0");
bool exitOnCatchUp = bool.Parse(config["Simulator:ExitOnCatchUp"] ?? "false");

if (speed <= 0) speed = 1;
if (hoursBack < 0) hoursBack = 0;

var rnd = new Random();

// -------------------------------------------------
// 설비 목록 읽기
// -------------------------------------------------
var equips = new List<Equip>();

try
{
    using var conn = new SqlConnection(connStr);
    conn.Open();

    using var cmd = new SqlCommand("SELECT EQUIP_CD, EQUIP_NM FROM dbo.MST_EQUIP WHERE USE_YN = 'Y' ORDER BY EQUIP_CD", conn);
    using var reader = cmd.ExecuteReader();

    while (reader.Read())
    {
        equips.Add(new Equip(reader.GetString(0), reader.GetString(1)));
    }
}
catch (Exception ex)
{
    Console.WriteLine("DB 접속 실패: ", ex.Message);
    Console.WriteLine("appsettings.json의 Server를 확인하세요.");
    return;
}

if(equips.Count == 0)
{
    Console.WriteLine("설비가 없습니다.");
    return;
}

// -------------------------------------------------
// 가상의 시각 준비
// -------------------------------------------------
var realStart = DateTime.Now;
var virtualStart = DateTime.Now.AddHours(-hoursBack);

DateTime VirtualNow()
{
    return virtualStart.AddSeconds((DateTime.Now - realStart).TotalSeconds * speed);
}

// 설비마다 첫 샷 시각과 상태 변경 시각을 흩뿌린다 (동시에 몰리지 않게)
foreach (var e in equips)
{
    e.CycleSec = 20 + rnd.NextDouble() * 15;                       // 20~35초
    e.NextShotAt = virtualStart.AddSeconds(rnd.Next(0, 30));
    e.StateUntil = virtualStart.AddMinutes(rnd.Next(20, 90));        // 20~90분 뒤 정지
}

Console.WriteLine($"설비 {equips.Count}대 / 배속 {speed}x / 시작 시각 {virtualStart:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine(exitOnCatchUp ? "현재 시각을 따라잡으면 자동으로 종료합니다." : "종료하려면 Ctrl + C");
Console.WriteLine(new string('-', 60));


// -------------------------------------------------
// 메인 루프
// -------------------------------------------------
const string INSERT_SQL = @"
    INSERT INTO dbo.TB_SHOT_LOG (EQUIP_CD, RESULT_ID, SHOT_DT, CYCLE_SEC)
    VALUES (@EquipCd, @ResultId, @ShotDt, @CycleSec)";

var runningInfo = new Dictionary<string, (long ResultId, double CycleSec)>();
var lastRefresh = DateTime.MinValue;
var lastSummary = DateTime.Now;
long totalShots = 0;
bool verbose = speed <= 5;   // 배속이 높으면 한 줄씩 찍지 않고 요약만

while (true)
{
    var now = VirtualNow();

    // 가상 시각이 현재를 따라잡으면 실시간 모드로 전환
    if (now > DateTime.Now)
    {
        if (speed != 1)
        {
            Console.WriteLine(">>> 현재 시각을 따라잡았습니다.");

            if (exitOnCatchUp)
            {
                Console.WriteLine($">>> 총 {totalShots:N0}건 적재. 종료합니다.");
                break;
            }

            Console.WriteLine(">>> 실시간 모드로 전환합니다.");
            speed = 1;
            verbose = true;
        }
        realStart = DateTime.Now;
        virtualStart = DateTime.Now;
        now = DateTime.Now;
    }

    // 진행중 실적 정보를 10초(실제시간)마다 한 번씩만 갱신
    if ((DateTime.Now - lastRefresh).TotalSeconds >= 10)
    {
        RefreshRunningResults(connStr, runningInfo);
        lastRefresh = DateTime.Now;
    }

    foreach (var e in equips)
    {
        // --- 가동 / 정지 상태 전환 ---
        if (now >= e.StateUntil)
        {
            e.Running = !e.Running;
            e.StateUntil = e.Running
                ? now.AddMinutes(rnd.Next(20, 90))   // 가동 20~90분
                : now.AddMinutes(rnd.Next(3, 15));   // 정지 3~15분

            if (verbose)
                Console.WriteLine($"[{now:HH:mm:ss}] {e.Code}  {(e.Running ? "정지 -> 가동" : "가동 -> 정지")}");

            if (e.Running) e.NextShotAt = now.AddSeconds(e.CycleSec);
        }

        if (!e.Running || now < e.NextShotAt) continue;

        // --- 샷 발생 ---
        // 진행중 실적이 있으면 그 품목의 표준 사이클타임을 따른다.
        long? resultId = null;
        double baseCycle = e.CycleSec;
        if (runningInfo.TryGetValue(e.Code, out var info))
        {
            resultId = info.ResultId;
            baseCycle = info.CycleSec;
        }

        // 실제 설비는 매번 똑같지 않다. ±8% 정도 흔들리게 한다.
        double actualCycle = Math.Round(baseCycle * (0.92 + rnd.NextDouble() * 0.16), 2);

        try
        {
            // 작업할 때마다 연결을 연다.
            // 커넥션 풀이 실제 연결을 재사용하므로 비용이 거의 없고,
            // 연결이 끊겼더라도 여기서 새로 받아온다.
            using var conn = new SqlConnection(connStr);
            conn.Open();

            using var cmd = new SqlCommand(INSERT_SQL, conn);
            cmd.Parameters.AddWithValue("@EquipCd", e.Code);
            cmd.Parameters.AddWithValue("@ResultId", (object?)resultId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ShotDt", now);
            cmd.Parameters.AddWithValue("@CycleSec", actualCycle);
            cmd.ExecuteNonQuery();

            e.ShotCount++;
            totalShots++;
            e.NextShotAt = now.AddSeconds(actualCycle);

            if (verbose)
                Console.WriteLine($"[{now:HH:mm:ss}] {e.Code}  샷 #{e.ShotCount}  ({actualCycle:F1}초)");
        }
        catch (Exception ex)
        {
            // 실패해도 멈추지 않는다. 다음 주기에 다시 시도한다.
            Console.WriteLine($"[{now:HH:mm:ss}] {e.Code}  INSERT 실패: {ex.Message}");
            e.NextShotAt = now.AddSeconds(baseCycle);
        }
    }

    // 배속 모드일 때는 2초마다 요약만 출력
    if (!verbose && (DateTime.Now - lastSummary).TotalSeconds >= 2)
    {
        Console.WriteLine($"[{now:MM-dd HH:mm}] 누적 {totalShots:N0}건  " +
                          $"(가동 {equips.Count(x => x.Running)}대 / 전체 {equips.Count}대)");
        lastSummary = DateTime.Now;
    }

    Thread.Sleep(200);
}

// ------------------------------------------------------------
// 진행중인 실적을 읽어 설비별로 매핑한다.
// ------------------------------------------------------------
static void RefreshRunningResults(
    string connStr,
    Dictionary<string, (long ResultId, double CycleSec)> map)
{
    const string sql = @"
        SELECT  R.EQUIP_CD, R.RESULT_ID, I.STD_CYCLE_SEC
        FROM        dbo.TB_WORK_RESULT R
        INNER JOIN  dbo.TB_WORK_ORDER  W ON W.WO_NO   = R.WO_NO
        INNER JOIN  dbo.MST_ITEM       I ON I.ITEM_CD = W.ITEM_CD
        WHERE R.RESULT_STATUS = 'RUN'";

    try
    {
        using var conn = new SqlConnection(connStr);
        conn.Open();

        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        // 조회에 성공한 뒤에 교체한다.
        // 먼저 map.Clear() 를 하면 조회가 실패했을 때 기존 정보까지 날아가고,
        // 그 사이의 샷 로그가 RESULT_ID 없이 적재된다.
        var fresh = new Dictionary<string, (long, double)>();
        while (reader.Read())
        {
            fresh[reader.GetString(0)] = (reader.GetInt64(1), (double)reader.GetDecimal(2));
        }

        map.Clear();
        foreach (var kv in fresh)
        {
            map[kv.Key] = kv.Value;
        }
    }
    catch (Exception ex)
    {
        // 실패해도 기존 map 을 유지한다. 잠깐 끊긴 것뿐일 수 있다.
        Console.WriteLine("진행중 실적 조회 실패: " + ex.Message);
    }
}

class Equip(string code, string name)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
    public bool Running { get; set; } = true;
    public double CycleSec { get; set; } = 25;
    public DateTime NextShotAt { get; set; }
    public DateTime StateUntil { get; set; }
    public int ShotCount { get; set; }
}