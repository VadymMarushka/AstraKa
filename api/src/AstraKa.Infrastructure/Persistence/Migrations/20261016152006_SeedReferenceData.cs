using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AstraKa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "Exercises",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Exercises",
                columns: new[] { "Id", "AuthorId", "Description", "Instructions", "Name", "UnitId", "VideoUrl" },
                values: new object[,]
                {
                    { new Guid("169f5b0a-9451-4222-8115-b0d881cdeec6"), null, "Give something to the sky. Do not ask what. It will know.", "Stand facing home with the palms joined at the chest. Slowly extend the arms forward with the palms turned up, as if giving something away, then draw the hands back to the chest. About 4 seconds out, 4 back. One rep = out and back.", "First Offering", (byte)1, null },
                    { new Guid("32b2e728-63fb-4379-a642-308c06f925b0"), null, "The stars hum at a frequency only the aligned can hear. Begin by humming at it.", "Sit cross-legged facing home with the hands on the knees. Inhale through the nose for 4 seconds. Exhale for 6 seconds while humming one low steady note. Keep the jaw loose and the volume quiet. Never hold the breath.", "Resonance", (byte)3, null },
                    { new Guid("401175c3-8c76-4727-8b87-10c330c89e91"), null, "Breathe on the flame, but do not extinguish it. Home is not that kind of place.", "Sit cross-legged facing home. Hold one index finger upright in front of the face, pointing toward home like a candle. Inhale through the nose for 4 seconds, then exhale slowly through pursed lips so the finger-flame would sway but not go out. Never hold the breath.", "Steady Flame", (byte)3, null },
                    { new Guid("53035a10-7991-4649-90e9-4dba38cc3231"), null, "Fall slowly, so the Earth has time to prepare. Lie as a star lies. Rise slowly, so it has time to notice.", "Stand facing home. Slowly lower yourself to kneeling, then sit, then lie down on your back with the arms and legs spread wide like a star, the gaze turned toward home. Pause for 2 seconds, then rise the same way. Use a hand on the floor for support if needed. One rep = down and up.", "Falling Star", (byte)1, null },
                    { new Guid("8975cb3e-32d5-4e2d-9eaa-546a20a12b13"), null, "Let the sky rise and fall inside you. It is only borrowing the space.", "Lie on your back with the head toward home and the knees bent, one hand on the chest and the other on the belly. Inhale through the nose for 4 seconds so the belly rises, then exhale for 6 seconds so it falls. Never hold the breath. One breath = one rise and fall.", "Quiet Tide", (byte)3, null },
                    { new Guid("ada984f7-18c0-407d-85db-f2e4a345467a"), null, "Be the line along which the light travels. Point at home, not near it.", "Lie on your side with your legs stacked, supported on the forearm beneath you. Lift the hips so the body forms one straight line. Extend the free arm straight forward and point it toward home. Hold, then switch sides at the halfway point. Lower the bottom knee to the floor if the line breaks.", "Beacon", (byte)2, null },
                    { new Guid("b12345be-0505-40ff-acb3-a385ce5b53a0"), null, "Become the bridge by which the soul crosses back. Open your palms; it may wish to carry something.", "Lie on your back, knees bent, feet flat and hip-width apart, head toward home. Rest the arms along the body with the palms open and facing up. Lift the hips until knees, hips and shoulders form one line. Hold while breathing evenly.", "Span", (byte)2, null },
                    { new Guid("c7e7a3ad-2d18-4966-9bce-c0c69ca2d8c0"), null, "Circle the center without touching it. Do not look away from home; it has noticed before.", "Sit cross-legged with the hands on the knees. Slowly circle the upper body around the center of the spine, about 6 seconds per circle, while the gaze stays fixed on home. Reverse the direction at the halfway point.", "Orbit", (byte)2, null },
                    { new Guid("cb78039e-b011-4856-b6f1-1955674690ce"), null, "Bow to the place you cannot see. It has seen you.", "Kneel facing home. Fold forward with the arms extended toward home and the forehead resting on the floor. Slowly return to kneeling with the hands on the knees. One rep = bow and return.", "Homeward Bow", (byte)1, null },
                    { new Guid("df53500d-0502-494c-80f4-5074add1199f"), null, "Stand on one leg, as the watchers do. The other leg is not needed tonight.", "Stand facing home next to a wall or chair. Place the sole of one foot against the inner calf or ankle of the standing leg (never against the knee), palms together at the chest, gaze on the horizon. Hold, then switch legs at the halfway point. Touch the support if you wobble.", "Lone Watcher", (byte)2, null },
                    { new Guid("f621908e-c663-468f-872d-5de06632e0a4"), null, "Point at the highest place in the sky above home. Return only when it has been located.", "Stand facing home with the feet hip-width apart. Raise both arms diagonally forward and up, reaching toward home, with the index fingers pointing and the gaze following them. Rise onto the toes at the same time. Lower slowly, about 4 seconds up and 4 down. One rep = up and down.", "Zenith Reach", (byte)1, null },
                    { new Guid("fad9002c-ec63-4853-ae89-cd478db86811"), null, "One finger points to the zenith, the other to the ground. Between them there is only you. Keep the line straight.", "Stand facing home with the feet wide. Raise one arm diagonally forward and up, and lower the other diagonally back and down, so that both arms form one straight line through the body. Point with the index fingers and keep the gaze on the upper finger. Switch the arms at the halfway point.", "Meridian", (byte)2, null }
                });

            migrationBuilder.InsertData(
                table: "ZodiacSigns",
                columns: new[] { "Id", "Name", "SequenceNumber" },
                values: new object[,]
                {
                    { 1, "Aries", 1 },
                    { 2, "Taurus", 2 },
                    { 3, "Gemini", 3 },
                    { 4, "Cancer", 4 },
                    { 5, "Leo", 5 },
                    { 6, "Virgo", 6 },
                    { 7, "Libra", 7 },
                    { 8, "Scorpio", 8 },
                    { 9, "Sagittarius", 9 },
                    { 10, "Capricorn", 10 },
                    { 11, "Aquarius", 11 },
                    { 12, "Pisces", 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("169f5b0a-9451-4222-8115-b0d881cdeec6"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("32b2e728-63fb-4379-a642-308c06f925b0"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("401175c3-8c76-4727-8b87-10c330c89e91"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("53035a10-7991-4649-90e9-4dba38cc3231"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("8975cb3e-32d5-4e2d-9eaa-546a20a12b13"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("ada984f7-18c0-407d-85db-f2e4a345467a"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("b12345be-0505-40ff-acb3-a385ce5b53a0"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("c7e7a3ad-2d18-4966-9bce-c0c69ca2d8c0"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("cb78039e-b011-4856-b6f1-1955674690ce"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("df53500d-0502-494c-80f4-5074add1199f"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("f621908e-c663-468f-872d-5de06632e0a4"));

            migrationBuilder.DeleteData(
                table: "Exercises",
                keyColumn: "Id",
                keyValue: new Guid("fad9002c-ec63-4853-ae89-cd478db86811"));

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ZodiacSigns",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "Exercises");
        }
    }
}
