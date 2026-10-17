using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Infrastructure.Persistence.Seeding;

/// <summary>
/// Test/demo data for Wanderers, Starshepherds, courses, cohorts, enrollments and daily activity -
/// everything EF Core owns except the reference catalog (<c>ZodiacSign</c>, <c>Exercise</c>), which
/// is seeded via <c>HasData</c> instead (see the "SeedReferenceData" migration). Wired up through
/// <c>UseSeeding</c>/<c>UseAsyncSeeding</c> in <see cref="DependencyInjection.AddInfrastructure"/>,
/// which EF Core 9+ runs as part of both <c>EnsureCreated</c> and `dotnet ef database update`.
/// </summary>
internal static class TestDataSeeder
{
    public static void Seed(DbContext context, bool _) =>
        SeedAsync(context, _, CancellationToken.None).GetAwaiter().GetResult();

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken ct)
    {
        if (context is not AppDbContext db)
        {
            return;
        }

        // Idempotent: never re-seed on top of an already-populated database.
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        // Every date below is an offset from the seeding run, never a hardcoded calendar date, so
        // cohort phases (finished / running / upcoming) stay correct no matter when this runs.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // The system exercise catalog is already in the database (seeded by the earlier
        // "SeedReferenceData" migration, which EF Core applies before this seeding step runs).
        var exerciseIds = await db.Exercises.ToDictionaryAsync(e => e.Name, e => e.Id, ct);

        // ---------- People ----------

        var oracle = new User(Guid.NewGuid(), "auth0|oracle", "oracle@astraka.app", "The Oracle", UserRole.Oracle);

        var miraUser = new User(Guid.NewGuid(), "auth0|mira-okonkwo", "mira.okonkwo@astraka.app", "Mira Okonkwo", UserRole.Starshepherd);
        var mira = new Starshepherd(miraUser.Id, "Grand Keeper of the Seventh Orbit");

        var desmondUser = new User(Guid.NewGuid(), "auth0|desmond-vale", "desmond.vale@astraka.app", "Desmond Vale", UserRole.Starshepherd);
        var desmond = new Starshepherd(desmondUser.Id, "Warden of the Low Horizon");

        var priyaUser = new User(Guid.NewGuid(), "auth0|priya-natarajan", "priya.natarajan@astraka.app", "Priya Natarajan", UserRole.Starshepherd);
        var priya = new Starshepherd(priyaUser.Id, "Custodian of Returning Light");

        var tomaszUser = new User(Guid.NewGuid(), "auth0|tomasz-rienko", "tomasz.rienko@astraka.app", "Tomasz Rienko", UserRole.Wanderer);
        var tomasz = new Wanderer(tomaszUser.Id, 1); // Aries

        var esmeUser = new User(Guid.NewGuid(), "auth0|esme-clarke", "esme.clarke@astraka.app", "Esme Clarke", UserRole.Wanderer);
        var esme = new Wanderer(esmeUser.Id, 4); // Cancer

        var hugoUser = new User(Guid.NewGuid(), "auth0|hugo-beltran", "hugo.beltran@astraka.app", "Hugo Beltrán", UserRole.Wanderer);
        var hugo = new Wanderer(hugoUser.Id, 4); // Cancer

        var naledUser = new User(Guid.NewGuid(), "auth0|naledi-dube", "naledi.dube@astraka.app", "Naledi Dube", UserRole.Wanderer);
        var naledi = new Wanderer(naledUser.Id, 5); // Leo

        var yukiUser = new User(Guid.NewGuid(), "auth0|yuki-tanaka", "yuki.tanaka@astraka.app", "Yuki Tanaka", UserRole.Wanderer);
        var yuki = new Wanderer(yukiUser.Id, 7); // Libra

        var oisinUser = new User(Guid.NewGuid(), "auth0|oisin-farrell", "oisin.farrell@astraka.app", "Oisín Farrell", UserRole.Wanderer);
        var oisin = new Wanderer(oisinUser.Id, 8); // Scorpio

        var ingridUser = new User(Guid.NewGuid(), "auth0|ingrid-solberg", "ingrid.solberg@astraka.app", "Ingrid Solberg", UserRole.Wanderer);
        var ingrid = new Wanderer(ingridUser.Id, 10); // Capricorn

        var amaraUser = new User(Guid.NewGuid(), "auth0|amara-osei", "amara.osei@astraka.app", "Amara Osei", UserRole.Wanderer);
        var amara = new Wanderer(amaraUser.Id, 12); // Pisces

        db.Users.AddRange(oracle, miraUser, desmondUser, priyaUser, tomaszUser, esmeUser, hugoUser, naledUser, yukiUser, oisinUser, ingridUser, amaraUser);
        db.Starshepherds.AddRange(mira, desmond, priya);
        db.Wanderers.AddRange(tomasz, esme, hugo, naledi, yuki, oisin, ingrid, amara);

        // ---------- Courses, days, day exercises ----------

        // HOMEWARD - any sign, 5 days: exercises, exercises, meeting-only, exercises, exercises.
        var homeward = new Course(Guid.NewGuid(), mira.UserId, "HOMEWARD", "Basic alignment for newcomers.", "You will know which way is home.", 19.99m);

        var hwDay1 = new CourseDay(Guid.NewGuid(), homeward.Id, 1, "Soul's departure from Earth orbit");
        var hwDay1Ex = AddExercises(db, hwDay1.Id, (exerciseIds["Quiet Tide"], 2, 6), (exerciseIds["Steady Flame"], 2, 6));

        var hwDay2 = new CourseDay(Guid.NewGuid(), homeward.Id, 2, "First contact with gravity");
        var hwDay2Ex = AddExercises(db, hwDay2.Id, (exerciseIds["Span"], 3, 30), (exerciseIds["Homeward Bow"], 3, 8));

        var hwDay3 = new CourseDay(Guid.NewGuid(), homeward.Id, 3, "Orbital check-in", meetLink: "https://meet.astraka.app/homeward/day-3");

        var hwDay4 = new CourseDay(Guid.NewGuid(), homeward.Id, 4, "Finding true north");
        var hwDay4Ex = AddExercises(db, hwDay4.Id, (exerciseIds["Lone Watcher"], 2, 20), (exerciseIds["Zenith Reach"], 3, 8));

        var hwDay5 = new CourseDay(Guid.NewGuid(), homeward.Id, 5, "Alignment, probably");
        var hwDay5Ex = AddExercises(db, hwDay5.Id, (exerciseIds["Beacon"], 2, 25), (exerciseIds["First Offering"], 3, 8));

        // Days go on the Course's own navigation (not just the DbSet) so Publish() - which requires
        // at least one day - sees them.
        foreach (var day in new[] { hwDay1, hwDay2, hwDay3, hwDay4, hwDay5 })
        {
            homeward.CourseDays.Add(day);
        }

        homeward.Publish();
        db.Courses.Add(homeward);

        // For Cancers - Cancer only, 4 days: exercises, exercises, meeting-only, exercises.
        var forCancers = new Course(Guid.NewGuid(), desmond.UserId, "For Cancers", "A program written for the home-feeling sign.", "You will feel less like you forgot something.", 24.99m);
        var forCancersSign = new CourseSign(forCancers.Id, 4); // Cancer

        var fcDay1 = new CourseDay(Guid.NewGuid(), forCancers.Id, 1, "Returning to the shell");
        var fcDay1Ex = AddExercises(db, fcDay1.Id, (exerciseIds["Span"], 3, 30), (exerciseIds["Orbit"], 2, 30));

        var fcDay2 = new CourseDay(Guid.NewGuid(), forCancers.Id, 2, "Tides and other excuses");
        var fcDay2Ex = AddExercises(db, fcDay2.Id, (exerciseIds["Quiet Tide"], 2, 6), (exerciseIds["Falling Star"], 3, 8));

        var fcDay3 = new CourseDay(Guid.NewGuid(), forCancers.Id, 3, "Meeting with your Starshepherd", meetLink: "https://meet.astraka.app/for-cancers/day-3");

        var fcDay4 = new CourseDay(Guid.NewGuid(), forCancers.Id, 4, "Full moon accountability");
        AddExercises(db, fcDay4.Id, (exerciseIds["Meridian"], 2, 30), (exerciseIds["Resonance"], 2, 6));

        foreach (var day in new[] { fcDay1, fcDay2, fcDay3, fcDay4 })
        {
            forCancers.CourseDays.Add(day);
        }

        forCancers.Publish();
        db.Courses.Add(forCancers);
        db.CourseSigns.Add(forCancersSign);

        // Mercury Retrograde Survival - any sign, 3 days: exercises, exercises, exercises.
        var mercury = new Course(Guid.NewGuid(), priya.UserId, "Mercury Retrograde Survival", "Survive the retrograde with your orbit intact.", "Nothing will get worse while you are aligned.", 14.99m);

        var mrDay1 = new CourseDay(Guid.NewGuid(), mercury.Id, 1, "Everything is late, including this");
        var mrDay1Ex = AddExercises(db, mrDay1.Id, (exerciseIds["Steady Flame"], 2, 6), (exerciseIds["Zenith Reach"], 3, 8));

        var mrDay2 = new CourseDay(Guid.NewGuid(), mercury.Id, 2, "Do not sign anything today");
        var mrDay2Ex = AddExercises(db, mrDay2.Id, (exerciseIds["Orbit"], 2, 30), (exerciseIds["Homeward Bow"], 3, 8));

        var mrDay3 = new CourseDay(Guid.NewGuid(), mercury.Id, 3, "It's probably over now");
        AddExercises(db, mrDay3.Id, (exerciseIds["Span"], 3, 30), (exerciseIds["First Offering"], 3, 8));

        foreach (var day in new[] { mrDay1, mrDay2, mrDay3 })
        {
            mercury.CourseDays.Add(day);
        }

        mercury.Publish();
        db.Courses.Add(mercury);

        // Saturn Return Recovery - Capricorn/Aquarius/Pisces, 4 days: exercises, exercises, meeting-only, exercises.
        var saturnReturn = new Course(Guid.NewGuid(), desmond.UserId, "Saturn Return Recovery", "For those approaching 29 whose life is falling apart.", "It will still be falling apart, but on schedule.", 29.99m);
        var srSigns = new[] { new CourseSign(saturnReturn.Id, 10), new CourseSign(saturnReturn.Id, 11), new CourseSign(saturnReturn.Id, 12) }; // Capricorn, Aquarius, Pisces

        var srDay1 = new CourseDay(Guid.NewGuid(), saturnReturn.Id, 1, "The reckoning begins");
        AddExercises(db, srDay1.Id, (exerciseIds["Falling Star"], 3, 8), (exerciseIds["Beacon"], 3, 25));

        var srDay2 = new CourseDay(Guid.NewGuid(), saturnReturn.Id, 2, "Rebuilding, slowly");
        AddExercises(db, srDay2.Id, (exerciseIds["Lone Watcher"], 2, 20), (exerciseIds["Zenith Reach"], 3, 8));

        var srDay3 = new CourseDay(Guid.NewGuid(), saturnReturn.Id, 3, "Check-in with your Starshepherd", meetLink: "https://meet.astraka.app/saturn-return/day-3");

        var srDay4 = new CourseDay(Guid.NewGuid(), saturnReturn.Id, 4, "Emerging, allegedly wiser");
        AddExercises(db, srDay4.Id, (exerciseIds["Homeward Bow"], 3, 8), (exerciseIds["Steady Flame"], 2, 6));

        foreach (var day in new[] { srDay1, srDay2, srDay3, srDay4 })
        {
            saturnReturn.CourseDays.Add(day);
        }

        saturnReturn.Publish();
        db.Courses.Add(saturnReturn);
        db.CourseSigns.AddRange(srSigns);

        // ---------- Cohorts ----------

        var homewardA = new Cohort(Guid.NewGuid(), homeward.Id, today.AddDays(-40), 10); // finished
        var homewardB = new Cohort(Guid.NewGuid(), homeward.Id, today.AddDays(14), 10); // upcoming
        var forCancersCohort = new Cohort(Guid.NewGuid(), forCancers.Id, today.AddDays(-2), 6); // running
        var mercuryCohort = new Cohort(Guid.NewGuid(), mercury.Id, today.AddDays(-1), 15); // running
        var saturnReturnCohort = new Cohort(Guid.NewGuid(), saturnReturn.Id, today.AddDays(7), 3); // upcoming, 1 free seat left

        db.Cohorts.AddRange(homewardA, homewardB, forCancersCohort, mercuryCohort, saturnReturnCohort);

        // ---------- Enrollments ----------

        Enrollment Enroll(Wanderer wanderer, Cohort cohort, decimal price, DateTime enrolledAt)
        {
            cohort.TakeSeat();
            var enrollment = new Enrollment(wanderer.UserId, cohort.Id, price, enrolledAt);
            db.Enrollments.Add(enrollment);
            return enrollment;
        }

        var hwaTomasz = Enroll(tomasz, homewardA, homeward.Price, homewardA.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));
        var hwaEsme = Enroll(esme, homewardA, homeward.Price, homewardA.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));
        var hwaNaledi = Enroll(naledi, homewardA, homeward.Price, homewardA.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));
        var hwaYuki = Enroll(yuki, homewardA, homeward.Price, homewardA.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));
        var hwaIngrid = Enroll(ingrid, homewardA, homeward.Price, homewardA.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));

        Enroll(hugo, homewardB, homeward.Price, now);
        Enroll(oisin, homewardB, homeward.Price, now);

        var fcEsme = Enroll(esme, forCancersCohort, forCancers.Price, forCancersCohort.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));
        var fcHugo = Enroll(hugo, forCancersCohort, forCancers.Price, forCancersCohort.StartDate.AddDays(-5).ToDateTime(TimeOnly.MinValue));

        var mrTomasz = Enroll(tomasz, mercuryCohort, mercury.Price, mercuryCohort.StartDate.AddDays(-3).ToDateTime(TimeOnly.MinValue));
        var mrNaledi = Enroll(naledi, mercuryCohort, mercury.Price, mercuryCohort.StartDate.AddDays(-3).ToDateTime(TimeOnly.MinValue));
        var mrOisin = Enroll(oisin, mercuryCohort, mercury.Price, mercuryCohort.StartDate.AddDays(-3).ToDateTime(TimeOnly.MinValue));
        var mrIngrid = Enroll(ingrid, mercuryCohort, mercury.Price, mercuryCohort.StartDate.AddDays(-3).ToDateTime(TimeOnly.MinValue));
        var mrAmara = Enroll(amara, mercuryCohort, mercury.Price, mercuryCohort.StartDate.AddDays(-3).ToDateTime(TimeOnly.MinValue));

        Enroll(ingrid, saturnReturnCohort, saturnReturn.Price, now);
        Enroll(amara, saturnReturnCohort, saturnReturn.Price, now);

        // ---------- Daily reports + completions - deliberately uneven ----------

        void Report(Enrollment enrollment, DateOnly date, byte harmonyLevel, List<Guid> dayExerciseIds, bool full)
        {
            var report = new DailyReport(Guid.NewGuid(), enrollment.WandererId, enrollment.CohortId, date, harmonyLevel);
            var toComplete = full ? dayExerciseIds : dayExerciseIds.Take(1).ToList();
            var completedAt = date.ToDateTime(TimeOnly.FromDateTime(now));
            foreach (var dayExerciseId in toComplete)
            {
                report.AddCompletion(dayExerciseId, dayExerciseIds, completedAt);
            }

            db.DailyReports.Add(report);
        }

        // HOMEWARD - Cohort A (finished; day 3 is meeting-only, skipped by everyone).
        var hwaDay1Date = hwDay1.GetDate(homewardA.StartDate);
        var hwaDay2Date = hwDay2.GetDate(homewardA.StartDate);
        var hwaDay4Date = hwDay4.GetDate(homewardA.StartDate);
        var hwaDay5Date = hwDay5.GetDate(homewardA.StartDate);

        // Tomasz - steady improver.
        Report(hwaTomasz, hwaDay1Date, 3, hwDay1Ex, full: true);
        Report(hwaTomasz, hwaDay2Date, 5, hwDay2Ex, full: true);
        Report(hwaTomasz, hwaDay4Date, 7, hwDay4Ex, full: true);
        Report(hwaTomasz, hwaDay5Date, 9, hwDay5Ex, full: true);

        // Esme - dips back into Chaos mid-course: day 2 missed, day 4 partial and a Chaos-level dip.
        Report(hwaEsme, hwaDay1Date, 6, hwDay1Ex, full: true);
        Report(hwaEsme, hwaDay4Date, 2, hwDay4Ex, full: false);
        Report(hwaEsme, hwaDay5Date, 6, hwDay5Ex, full: true);

        // Naledi - ordinary partial completer, every day but never everything.
        Report(hwaNaledi, hwaDay1Date, 5, hwDay1Ex, full: false);
        Report(hwaNaledi, hwaDay2Date, 5, hwDay2Ex, full: false);
        Report(hwaNaledi, hwaDay4Date, 4, hwDay4Ex, full: false);
        Report(hwaNaledi, hwaDay5Date, 6, hwDay5Ex, full: false);

        // Yuki - reports days 1-2, then leaves; no activity afterwards.
        Report(hwaYuki, hwaDay1Date, 4, hwDay1Ex, full: true);
        Report(hwaYuki, hwaDay2Date, 3, hwDay2Ex, full: true);
        hwaYuki.Leave(CohortPhase.Running); // still running in-story, at the point she leaves

        // Ingrid - flat in Imbalance throughout.
        Report(hwaIngrid, hwaDay1Date, 5, hwDay1Ex, full: true);
        Report(hwaIngrid, hwaDay2Date, 5, hwDay2Ex, full: true);
        Report(hwaIngrid, hwaDay4Date, 5, hwDay4Ex, full: true);
        Report(hwaIngrid, hwaDay5Date, 5, hwDay5Ex, full: true);

        // For Cancers (running; days 1-2 elapsed, day 3 = today is meeting-only).
        var fcDay1Date = fcDay1.GetDate(forCancersCohort.StartDate);
        var fcDay2Date = fcDay2.GetDate(forCancersCohort.StartDate);

        Report(fcEsme, fcDay1Date, 6, fcDay1Ex, full: true); // day 2 missed
        Report(fcHugo, fcDay2Date, 6, fcDay2Ex, full: true); // day 1 missed - mirror case

        // Mercury (running; day 1 elapsed, day 2 = today).
        var mrDay1Date = mrDay1.GetDate(mercuryCohort.StartDate);
        var mrDay2Date = mrDay2.GetDate(mercuryCohort.StartDate);

        Report(mrTomasz, mrDay1Date, 6, mrDay1Ex, full: true);
        Report(mrTomasz, mrDay2Date, 7, mrDay2Ex, full: true);

        Report(mrNaledi, mrDay1Date, 4, mrDay1Ex, full: false); // day 2 not reported yet

        Report(mrOisin, mrDay2Date, 5, mrDay2Ex, full: true); // day 1 missed

        Report(mrIngrid, mrDay1Date, 5, mrDay1Ex, full: true);
        Report(mrIngrid, mrDay2Date, 5, mrDay2Ex, full: true);

        Report(mrAmara, mrDay1Date, 6, mrDay1Ex, full: true);
        Report(mrAmara, mrDay2Date, 6, mrDay2Ex, full: false); // still in progress today

        // ---------- Revelations with extra exercises ----------

        var fcDay4Date = fcDay4.GetDate(forCancersCohort.StartDate); // tomorrow - not yet passed
        var revelation1 = new Revelation(Guid.NewGuid(), esme.UserId, forCancersCohort.Id, fcDay4.Id,
            "Your shell looked a little thin today. Add this before the moon notices.", now, fcDay4Date, today);
        db.Revelations.Add(revelation1);
        db.DayExercises.Add(new DayExercise(Guid.NewGuid(), fcDay4.Id, exerciseIds["Beacon"], 3, 2, 20, revelation1.Id));

        var mrDay3Date = mrDay3.GetDate(mercuryCohort.StartDate); // tomorrow - not yet passed
        var revelation2 = new Revelation(Guid.NewGuid(), amara.UserId, mercuryCohort.Id, mrDay3.Id,
            "Mercury is not done yet. One more orbit before you rest.", now, mrDay3Date, today);
        db.Revelations.Add(revelation2);
        db.DayExercises.Add(new DayExercise(Guid.NewGuid(), mrDay3.Id, exerciseIds["Lone Watcher"], 3, 2, 20, revelation2.Id));

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Creates and tracks <see cref="DayExercise"/> rows for a day's base program, returning their ids.</summary>
    private static List<Guid> AddExercises(AppDbContext db, Guid courseDayId, params (Guid ExerciseId, int Sets, int Amount)[] exercises)
    {
        var ids = new List<Guid>();
        for (var i = 0; i < exercises.Length; i++)
        {
            var (exerciseId, sets, amount) = exercises[i];
            var dayExercise = new DayExercise(Guid.NewGuid(), courseDayId, exerciseId, i + 1, sets, amount);
            db.DayExercises.Add(dayExercise);
            ids.Add(dayExercise.Id);
        }

        return ids;
    }
}
