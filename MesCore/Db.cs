using Microsoft.Data.SqlClient;
using System.Data;

namespace MesCore
{
    public static class Db
    {
        private static string _connStr = "";

        public static void Init(string connStr) => _connStr = connStr;

        private static SqlConnection Open()
        {
            var conn = new SqlConnection(_connStr);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// SELECT 결과를 원하는 클래스 목록으로 받는다.
        /// </summary>
        public static List<T> Query<T>(string sql, Func<IDataRecord, T> map, params (string Name, object? Value)[] parameters)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, parameters);

            var list = new List<T>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(map(reader));
            }
            return list;
        }

        /// <summary>SELECT - 값 하나만 받는다. (COUNT, MAX 등)</summary>
        public static T? Scalar<T>(string sql, params (string Name, object? Value)[] parameters)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, parameters);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull) return default;
            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>INSERT / UPDATE / DELETE - 바뀐 행 수를 돌려준다.</summary>
        public static int Execute(string sql, params (string Name, object? Value)[] parameters)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 저장 프로시저를 실행하고, 마지막 SELECT 한 줄을 받는다.
        /// 
        /// 이 프로젝트의 프로시저는 모두 마지막에
        ///   SELECT ResultCode, Message, (추가값)
        /// 을 돌려주도록 통일했다.
        /// </summary>
        public static T? Proc<T>(string procName, Func<IDataRecord, T> map, params (string Name, object? Value)[] parameters)
        {
            using var conn = Open();
            using var cmd = new SqlCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            AddParams(cmd, parameters);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? map(reader) : default;
        }

        private static void AddParams(SqlCommand cmd, (string Name, object? Value)[] parameters)
        {
            foreach (var p in parameters)
            {
                cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
            }
        }
    }
}
