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
            Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
        }
        
        try
        {
            wrapper.Stop();
        } 
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
        }
    }
}
