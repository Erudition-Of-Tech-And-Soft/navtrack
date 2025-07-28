
using Odoo.Navtrac.Api.Shared;

public class Program
{
    public static void Main(string[] args)
    {
        BaseApiProgram<Program>.Main(args, typeof(Program).Assembly);
    }
}