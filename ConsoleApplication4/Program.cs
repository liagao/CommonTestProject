using System;
using System.Text;

class Program
{

    static void Main(string[] args)
    {
        string str1 = "aaa";
        string str2 = "AAA";

        if (string.Equals(str1, str2.ToLower()))
        {
            Console.WriteLine("Exit");
        }

        throw new NullReferenceException();
    }

    public PluginResult Execute(PluginServices pluginServices)
    {
        /*pluginServices.Logger.Info(string.Format("16", "11")); 
        pluginServices.Logger.Info("123", "11");
        string.Concat("123", "456");
        StringBuilder ss = new StringBuilder();
        ss.Append("123");

        const int a = 13;
        string a1 = a.ToString(); 
        string.Equals(a1.ToLower(), "11");
        "123" + "456";*/
        const int a2345 = 13;
        string a1 = a2345.ToString();
        string result = string.Format("{0}", 123);   
        return null;
    }

    public string GetString()
    {
        return string.Empty;
    }

    public class PluginResult
    {
    }
}

public class PluginServices
{
    public string Name { get; set; }
    public string GetName()
    {
        return string.Empty;
    }
    public PluginLogger Logger { get; internal set; }
    public class PluginLogger
    {
        internal void Info(string format, params object[] array) {}

        internal void Info(string message) {}
    }
}