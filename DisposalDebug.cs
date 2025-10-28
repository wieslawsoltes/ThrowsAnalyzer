using System;
using System.IO;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new FileStream("test.txt", FileMode.Open))
        {
            // Do something
        }
    }
}
