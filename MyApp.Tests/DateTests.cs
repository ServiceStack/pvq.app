using NUnit.Framework;
using ServiceStack.OrmLite;

namespace MyApp.Tests;

public class DateTests
{
    public class DateModel
    {
        public int Id { get; set; }
        public DateTime Utc { get; set; }
        public DateTime Local { get; set; }
        public DateTime Unspecified { get; set; }
    }
    
    [Test]
    public void Can_store_and_retrieve_dates()
    {
        var dateConverter = SqliteDialect.Provider.GetDateTimeConverter();
        dateConverter.DateStyle = DateTimeKind.Utc;
        
        var dbFactory = new OrmLiteConnectionFactory("test.db", SqliteDialect.Provider);
        using var db = dbFactory.Open();
        db.DropAndCreateTable<DateModel>();
        
        var utc = DateTime.UtcNow;
        var local = DateTime.Now;
        var unspecified = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        
        var model = new DateModel {
            Id = 1,
            Utc = utc,
            Local = local,
            Unspecified = unspecified,
        };
        
        db.Insert(model);
        
        var fromDb = db.SingleById<DateModel>(model.Id);
        
        Assert.That(fromDb.Utc, Is.EqualTo(utc));
        // Assert.That(fromDb.Local, Is.EqualTo(local));
        Assert.That(fromDb.Unspecified, Is.EqualTo(unspecified));
    }
}