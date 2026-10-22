using System.Text.Json;
using AgeCheck.Core;

namespace AgeCheck.Tests;
public class CustomerFeaturesTests
{
    [Theory]
    [InlineData(2005, 10, 22, 2026, 10, 21, 20, 1, 1)]
    [InlineData(2005, 10, 22, 2026, 10, 22, 21, 0, -1)]
    [InlineData(2005, 10, 22, 2026, 10, 23, 21, 364, -1)]
    [InlineData(2007, 10, 22, 2026, 10, 22, 19, 0, 731)]
    [InlineData(2000, 12, 31, 2026, 1, 1, 25, 364, -1)]
    [InlineData(2004, 2, 29, 2025, 2, 28, 20, 1, 1)]
    [InlineData(2004, 2, 29, 2025, 3, 1, 21, 0, -1)]
    [InlineData(2004, 2, 29, 2028, 2, 28, 23, 1, -1)]
    public void CalendarDetailsHaveExactAgeAndCountdowns(int by, int bm, int bd, int ty, int tm, int td, int age, int birthday, int threshold)
    {
        var details = AgeDetails.From(new(by,bm,bd),new(ty,tm,td))!;
        Assert.Equal(age,details.Age); Assert.Equal(birthday,details.DaysUntilBirthday);
        Assert.Equal(threshold < 0 ? null : (int?)threshold, details.DaysUntil21);
        Assert.Equal(birthday == 0,details.BirthdayToday);
    }
    [Fact] public void FutureBirthDoesNotProduceAge() => Assert.Null(AgeDetails.From(new(2030,1,1),new(2026,1,1)));
    [Fact] public void SameNameDifferentCardsRemainDifferentCustomers()
    {
        var book = new CustomerBook(); var now = DateTimeOffset.UtcNow;
        Assert.False(book.Visit(new string('A',64), "ALEX SAMPLE", now).Returning);
        Assert.False(book.Visit(new string('B',64), "ALEX SAMPLE", now.AddMinutes(1)).Returning);
        var returning = book.Visit(new string('A',64), "ALEX SAMPLE", now.AddDays(1));
        Assert.True(returning.Returning); Assert.Equal(2,returning.Visits); Assert.Equal(now,returning.PreviousVisit);
        var reloaded = new CustomerBook(book.Records);
        Assert.True(reloaded.Visit(new string('A',64), "ALEX SAMPLE", now.AddDays(2)).Returning);
    }
    [Fact] public void CustomerBookPrunesOldestAtCapacity()
    {
        var now=DateTimeOffset.UtcNow; var book=new CustomerBook();
        for(int i=0;i<=CustomerBook.Capacity;i++) book.Visit(i.ToString("X64"),"ALEX SAMPLE",now.AddSeconds(i));
        Assert.Equal(CustomerBook.Capacity,book.Records.Count);
        Assert.DoesNotContain(book.Records,r=>r.Fingerprint==0.ToString("X64"));
    }
    [Fact] public void PrivateIdentityIsNeverJsonSerialized()
    {
        var r=Barcode.Check(Demo.CompleteKeyboard(new(1997,1,1),new(2030,1,1),new(2025,1,1)),new(2026,10,22),true);
        Assert.NotNull(r.CardIdentity); Assert.DoesNotContain(r.CardIdentity!,JsonSerializer.Serialize(r));
    }
    [Fact] public void HistoryKeepsAgeWithoutBirthDateOrIdentity()
    {
        var history=new SessionHistory(); var today=new DateOnly(2026,10,22);
        var result=Barcode.Check(Demo.CompleteKeyboard(new(2005,10,22),new(2030,1,1),new(2025,1,1)),today,true);
        history.Record(Guid.NewGuid(),result,false,false,DateTimeOffset.UtcNow,today,"Seen before");
        Assert.Equal(21,history.Entries[0].Age); Assert.Equal("Seen before",history.Entries[0].VisitLabel);
        string json=JsonSerializer.Serialize(history.Entries); Assert.DoesNotContain("CardIdentity",json); Assert.DoesNotContain("Birth",json);
    }
    [Fact] public void InvalidRestoredRecordsAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>new CustomerBook([new(null!,"ALEX",DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,1)]));
        Assert.Throws<ArgumentException>(()=>new CustomerBook([null!]));
    }
}
