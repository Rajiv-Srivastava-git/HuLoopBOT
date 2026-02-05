
namespace HuLoopBOT.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public static OperationResult Ok(string msg) => new() { Success = true, Message = msg };
    public static OperationResult Fail(string msg) => new() { Success = false, Message = msg };
}
