namespace HoscyCli;

class Program
{
    static void Main(string[] args)
    {
        var wrapper = new CliCoreWrapper();

        try
        {
            wrapper.Start();
            wrapper.RunLoop();
        } 
        catch (Exception e)
        {
            Util.DisplayEx(e);
        }
        
        try
        {
            wrapper.Stop();
        } 
        catch (Exception e)
        {
            Util.DisplayEx(e);
        }
    }
}
