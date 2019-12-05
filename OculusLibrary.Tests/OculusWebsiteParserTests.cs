using NSubstitute;
using NUnit.Framework;
using Playnite.SDK;
using System.IO;

namespace OculusLibrary.Tests
{
    public class OculusWebsiteParserTests
    {
        private IWebView fakeWebView;
        private OculusWebsiteScraper subject;

        [SetUp]
        public void Setup()
        {
            var testHtml = File.ReadAllText($"{TestContext.CurrentContext.TestDirectory}\\demo1.html");

            fakeWebView = Substitute.For<IWebView>();

            fakeWebView.GetPageSource()
                .Returns(testHtml);

            var fakeLogger = Substitute.For<ILogger>();

            subject = new OculusWebsiteScraper(fakeLogger);
        }

        [TearDown]
        public void TearDown() {
            fakeWebView = null;
            subject = null;
        }

        [Test]
        public void Game_Name_Correctly_Extracted()
        {
            var result = subject.ScrapeDataForApplicationId(fakeWebView, "123");

            Assert.AreEqual("Test Game", result.Name);
        }

        [Test]
        public void Game_Description_Correctly_Extracted()
        {
            var result = subject.ScrapeDataForApplicationId(fakeWebView, "123");

            Assert.AreEqual("This is a test description", result.Description);
        }
    }
}