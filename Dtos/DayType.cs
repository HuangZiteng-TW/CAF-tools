namespace CAF.Dtos;

public class DayType
{
    public DayType()
    {
        Result = new DayTypeDetail("");
    }

    public string Reason { get; set; }
    public DayTypeDetail Result { get; set; }
    public int ErrorCode { get; set; }
}

public class DayTypeDetail
{
    public DayTypeDetail(string statusDesc)
    {
        StatusDesc = statusDesc;
    }

    public string Date { get; set; }
    public string Week { get; set; }
    public string StatusDesc { get; set; }
    public string Status { get; set; }

    public bool isHoliday()
    {
        return StatusDesc != "工作日";
    }
}