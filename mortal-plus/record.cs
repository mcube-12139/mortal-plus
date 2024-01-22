class RecordListResult : BaseResult {
    public string[] data { get; set; }
}

class Record {
    public static RecordListResult getList() {
        return new RecordListResult() {
            data = {}
        };
    }
}
