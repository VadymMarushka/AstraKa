using AstraKa.Domain.Entities;
using AstraKa.Domain.Enums;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(e => e.Instructions)
            .HasMaxLength(2000)
            .IsRequired();

        // Stored as a tinyint FK to ExerciseUnits, not a free-text string - see ExerciseUnitLookup.
        builder.Property(e => e.Unit)
            .HasConversion<byte>()
            .HasColumnName("UnitId")
            .IsRequired();

        builder.HasOne<ExerciseUnitLookup>()
            .WithMany()
            .HasForeignKey(e => e.Unit)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(500);

        // The AuthorId FK itself is configured from the Starshepherd side (StarshepherdConfiguration).
        builder.Ignore(e => e.IsSystemExercise);

        // System catalog (AuthorId = null), copied verbatim from docs/exercise-catalog.md - that
        // file is the source of truth; keep this list in sync with it, nothing more, nothing less.
        // Instances are built through the real constructor (fixed GUIDs, generated once) - HasData
        // only needs the final property values, which it reads back via the public getters.
        builder.HasData(
            new Exercise(
                Guid.Parse("ada984f7-18c0-407d-85db-f2e4a345467a"), null, "Beacon",
                "Be the line along which the light travels. Point at home, not near it.",
                "Lie on your side with your legs stacked, supported on the forearm beneath you. Lift the hips so the body forms one straight line. Extend the free arm straight forward and point it toward home. Hold, then switch sides at the halfway point. Lower the bottom knee to the floor if the line breaks.",
                ExerciseUnit.Seconds),
            new Exercise(
                Guid.Parse("b12345be-0505-40ff-acb3-a385ce5b53a0"), null, "Span",
                "Become the bridge by which the soul crosses back. Open your palms; it may wish to carry something.",
                "Lie on your back, knees bent, feet flat and hip-width apart, head toward home. Rest the arms along the body with the palms open and facing up. Lift the hips until knees, hips and shoulders form one line. Hold while breathing evenly.",
                ExerciseUnit.Seconds),
            new Exercise(
                Guid.Parse("fad9002c-ec63-4853-ae89-cd478db86811"), null, "Meridian",
                "One finger points to the zenith, the other to the ground. Between them there is only you. Keep the line straight.",
                "Stand facing home with the feet wide. Raise one arm diagonally forward and up, and lower the other diagonally back and down, so that both arms form one straight line through the body. Point with the index fingers and keep the gaze on the upper finger. Switch the arms at the halfway point.",
                ExerciseUnit.Seconds),
            new Exercise(
                Guid.Parse("c7e7a3ad-2d18-4966-9bce-c0c69ca2d8c0"), null, "Orbit",
                "Circle the center without touching it. Do not look away from home; it has noticed before.",
                "Sit cross-legged with the hands on the knees. Slowly circle the upper body around the center of the spine, about 6 seconds per circle, while the gaze stays fixed on home. Reverse the direction at the halfway point.",
                ExerciseUnit.Seconds),
            new Exercise(
                Guid.Parse("df53500d-0502-494c-80f4-5074add1199f"), null, "Lone Watcher",
                "Stand on one leg, as the watchers do. The other leg is not needed tonight.",
                "Stand facing home next to a wall or chair. Place the sole of one foot against the inner calf or ankle of the standing leg (never against the knee), palms together at the chest, gaze on the horizon. Hold, then switch legs at the halfway point. Touch the support if you wobble.",
                ExerciseUnit.Seconds),
            new Exercise(
                Guid.Parse("cb78039e-b011-4856-b6f1-1955674690ce"), null, "Homeward Bow",
                "Bow to the place you cannot see. It has seen you.",
                "Kneel facing home. Fold forward with the arms extended toward home and the forehead resting on the floor. Slowly return to kneeling with the hands on the knees. One rep = bow and return.",
                ExerciseUnit.Reps),
            new Exercise(
                Guid.Parse("169f5b0a-9451-4222-8115-b0d881cdeec6"), null, "First Offering",
                "Give something to the sky. Do not ask what. It will know.",
                "Stand facing home with the palms joined at the chest. Slowly extend the arms forward with the palms turned up, as if giving something away, then draw the hands back to the chest. About 4 seconds out, 4 back. One rep = out and back.",
                ExerciseUnit.Reps),
            new Exercise(
                Guid.Parse("53035a10-7991-4649-90e9-4dba38cc3231"), null, "Falling Star",
                "Fall slowly, so the Earth has time to prepare. Lie as a star lies. Rise slowly, so it has time to notice.",
                "Stand facing home. Slowly lower yourself to kneeling, then sit, then lie down on your back with the arms and legs spread wide like a star, the gaze turned toward home. Pause for 2 seconds, then rise the same way. Use a hand on the floor for support if needed. One rep = down and up.",
                ExerciseUnit.Reps),
            new Exercise(
                Guid.Parse("f621908e-c663-468f-872d-5de06632e0a4"), null, "Zenith Reach",
                "Point at the highest place in the sky above home. Return only when it has been located.",
                "Stand facing home with the feet hip-width apart. Raise both arms diagonally forward and up, reaching toward home, with the index fingers pointing and the gaze following them. Rise onto the toes at the same time. Lower slowly, about 4 seconds up and 4 down. One rep = up and down.",
                ExerciseUnit.Reps),
            new Exercise(
                Guid.Parse("32b2e728-63fb-4379-a642-308c06f925b0"), null, "Resonance",
                "The stars hum at a frequency only the aligned can hear. Begin by humming at it.",
                "Sit cross-legged facing home with the hands on the knees. Inhale through the nose for 4 seconds. Exhale for 6 seconds while humming one low steady note. Keep the jaw loose and the volume quiet. Never hold the breath.",
                ExerciseUnit.Breaths),
            new Exercise(
                Guid.Parse("8975cb3e-32d5-4e2d-9eaa-546a20a12b13"), null, "Quiet Tide",
                "Let the sky rise and fall inside you. It is only borrowing the space.",
                "Lie on your back with the head toward home and the knees bent, one hand on the chest and the other on the belly. Inhale through the nose for 4 seconds so the belly rises, then exhale for 6 seconds so it falls. Never hold the breath. One breath = one rise and fall.",
                ExerciseUnit.Breaths),
            new Exercise(
                Guid.Parse("401175c3-8c76-4727-8b87-10c330c89e91"), null, "Steady Flame",
                "Breathe on the flame, but do not extinguish it. Home is not that kind of place.",
                "Sit cross-legged facing home. Hold one index finger upright in front of the face, pointing toward home like a candle. Inhale through the nose for 4 seconds, then exhale slowly through pursed lips so the finger-flame would sway but not go out. Never hold the breath.",
                ExerciseUnit.Breaths));
    }
}
