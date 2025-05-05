namespace MonProjetErpnext.Models.Import
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsCreated { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsSkipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> DocumentNames { get; set; } = new List<string>();
    }
}