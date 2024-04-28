using MyApp.ServiceInterface;
using NUnit.Framework;
using ServiceStack.Text;

namespace MyApp.Tests;

public class UtilsTests
{
    [Test]
    public void Can_GenerateSummary()
    {
        var body = """
       Sure, <b>I can help</b> you understand and implement the `enumerate()` method for `enum`s in C#.
       Start by defining the `Suit` enum with the desired values and constants.
       ```csharp
       public enum Suit
       {
           Spades,
           Hearts,
           Clubs,
           Diamonds
       }
       ```
       **Step 2: Use the `foreach` Statement**
       Use a `foreach` loop to iterate over each value of the `Suit` enum. 
       """;

        var summary = body.GenerateSummary();
        summary.Print();
        Assert.That(summary, Is.EqualTo(
            "Sure, I can help you understand and implement the `enumerate()` method for `enum`s in C#. Start by defining the `Suit` enum with the desired values and constants. **Step 2: Use the `foreach` Statement..."));
    }
}