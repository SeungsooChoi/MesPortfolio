namespace MesCore
{
    public class ItemRow
    {
        public string ItemCd { get; set; } = "";
        public string ItemNm { get; set; } = "";
        public string ItemType { get; set; } = "FG";
        public string Unit { get; set; } = "EA";
        public int Cavity { get; set; }
        public decimal StdCycleSec { get; set; }
        public string UseYn { get; set; } = "Y";

        /// <summary>
        ///  시간당 생산량 = (3600 / 사이클타임) x 캐비티
        ///  화면에서 계산한다.
        /// </summary>
        public int HourlyQty => StdCycleSec <= 0 ? 0 : (int)(3600m / StdCycleSec * Cavity);

        public bool IsUsed => UseYn == "Y";
    }

    public class EquipRow
    {
        public string EquipCd { get; set; } = "";
        public string EquipNm { get; set; } = "";
        public string? LineCd { get; set; }
        public int? Tonnage { get; set; }
        public string UseYn { get; set; } = "Y";

        public bool IsUsed => UseYn == "Y";
    }
}