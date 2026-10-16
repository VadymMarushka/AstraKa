using AstraKa.Infrastructure.Persistence;
using AstraKa.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Tests.Concurrency;

/// <summary>
/// Proves <c>Cohort.RowVersion</c> (configured as a concurrency token in CohortConfiguration) does
/// its job: two independent <see cref="AppDbContext"/> instances loading the same row, both
/// updating it, must not both succeed.
/// </summary>
[Collection("Database")]
public class ConcurrencyTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task TakeSeat_on_two_separate_DbContexts_the_second_save_throws_DbUpdateConcurrencyException()
    {
        // Saturn Return Recovery's cohort is seeded with exactly one free seat (SeatLimit 3, SeatsTaken 2).
        var cohortId = await fixture.DbContext.Cohorts
            .Where(c => c.Course.Name == "Saturn Return Recovery")
            .Select(c => c.Id)
            .SingleAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(fixture.ConnectionString).Options;

        using var context1 = new AppDbContext(options);
        using var context2 = new AppDbContext(options);

        var cohort1 = await context1.Cohorts.SingleAsync(c => c.Id == cohortId);
        var cohort2 = await context2.Cohorts.SingleAsync(c => c.Id == cohortId); // same RowVersion as cohort1

        cohort1.TakeSeat();
        await context1.SaveChangesAsync(); // commits: SeatsTaken 2 -> 3, RowVersion changes in the DB

        // cohort2's in-memory SeatsTaken is still the stale 2, so the domain guard alone would let
        // this through - only the RowVersion mismatch on context2's UPDATE catches the conflict.
        cohort2.TakeSeat();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }
}
