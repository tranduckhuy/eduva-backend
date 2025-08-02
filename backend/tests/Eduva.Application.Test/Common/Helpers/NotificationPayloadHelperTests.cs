using Eduva.Application.Common.Helpers;

namespace Eduva.Application.Test.Common.Helpers
{
    [TestFixture]
    public class NotificationPayloadHelperTests
    {
        #region Null/Empty Input Tests
        [Test]
        public void DeserializePayload_WithNullOrEmpty_ReturnsEmptyObject()
        {
            Assert.Multiple(() =>
            {
                Assert.That(NotificationPayloadHelper.DeserializePayload(null!), Is.Not.Null);
                Assert.That(NotificationPayloadHelper.DeserializePayload(""), Is.Not.Null);
                Assert.That(NotificationPayloadHelper.DeserializePayload("   "), Is.Not.Null);
            });
        }
        #endregion

        #region Valid JSON Type Tests
        [Test]
        public void DeserializePayload_WithObject_ReturnsDictionary()
        {
            var result = NotificationPayloadHelper.DeserializePayload("{\"name\":\"test\",\"value\":123}");
            var dict = (Dictionary<string, object>)result;
            Assert.Multiple(() =>
            {
                Assert.That(dict["name"], Is.EqualTo("test"));
                Assert.That(dict["value"], Is.EqualTo(123));
            });
        }

        [Test]
        public void DeserializePayload_WithArray_ReturnsList()
        {
            var result = NotificationPayloadHelper.DeserializePayload("[1,2,3]");
            var list = (List<object>)result;
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void DeserializePayload_WithString_ReturnsString()
        {
            var result = NotificationPayloadHelper.DeserializePayload("\"test\"");
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void DeserializePayload_WithNumber_ReturnsNumber()
        {
            var result = NotificationPayloadHelper.DeserializePayload("42");
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void DeserializePayload_WithLargeNumber_ReturnsLong()
        {
            var result = NotificationPayloadHelper.DeserializePayload("9223372036854775807");
            Assert.That(result, Is.EqualTo(9223372036854775807L));
        }

        [Test]
        public void DeserializePayload_WithDecimal_ReturnsDouble()
        {
            var result = NotificationPayloadHelper.DeserializePayload("3.14");
            Assert.That(result, Is.EqualTo(3.14));
        }

        [Test]
        public void DeserializePayload_WithBoolean_ReturnsBoolean()
        {
            Assert.Multiple(() =>
            {
                Assert.That(NotificationPayloadHelper.DeserializePayload("true"), Is.True);
                Assert.That(NotificationPayloadHelper.DeserializePayload("false"), Is.False);
            });
        }

        [Test]
        public void DeserializePayload_WithNull_ReturnsNull()
        {
            var result = NotificationPayloadHelper.DeserializePayload("null");
            Assert.That(result, Is.Null);
        }
        #endregion

        #region Error Handling Tests
        [Test]
        public void DeserializePayload_WithInvalidJson_ReturnsErrorObject()
        {
            var result = NotificationPayloadHelper.DeserializePayload("{\"invalid\": json}");
            var resultType = result.GetType();
            Assert.Multiple(() =>
            {
                Assert.That(resultType.GetProperty("error"), Is.Not.Null);
                Assert.That(resultType.GetProperty("originalPayload"), Is.Not.Null);
            });
        }
        #endregion

        #region Complex Structure Tests
        [Test]
        public void DeserializePayload_WithComplexNested_ReturnsDeserializedObject()
        {
            var json = "{\"user\":{\"id\":1,\"name\":\"John\"},\"items\":[1,2,3],\"metadata\":null}";
            var result = NotificationPayloadHelper.DeserializePayload(json);
            var dict = (Dictionary<string, object>)result;
            Assert.Multiple(() =>
            {
                Assert.That(dict.ContainsKey("user"), Is.True);
                Assert.That(dict.ContainsKey("items"), Is.True);
                Assert.That(dict.ContainsKey("metadata"), Is.True);
            });
        }
        #endregion

        #region Edge Cases Tests
        [Test]
        public void DeserializePayload_WithEmptyString_ReturnsEmptyString()
        {
            var result = NotificationPayloadHelper.DeserializePayload("\"\"");
            Assert.That(result, Is.EqualTo(""));
        }
        #endregion
    }
}