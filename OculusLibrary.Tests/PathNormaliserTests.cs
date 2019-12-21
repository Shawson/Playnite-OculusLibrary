using NSubstitute;
using NUnit.Framework;
using OculusLibrary.OS;
using System.Collections.Generic;

namespace OculusLibrary.Tests
{
    public class PathNormaliserTests
    {
        [Test]
        public void Get_Drive_Letter_From_DeviceId()
        {
            var path = @"\\?\Volume{a0bf4e34-c90f-4005-815c-9a5a485e40ad}\Oculus\Software";

            var fakeWMOProvider = Substitute.For<IWMODriveQueryProvider>();
            fakeWMOProvider.GetDriveData().Returns(new List<WMODrive> {
                new WMODrive {
                    DeviceId = @"\\?\Volume{DCBDB210-C414-409E-B108-C2BFA7395E1F}\",
                    DriveLetter = "X:"
                },
                new WMODrive {
                    DeviceId = @"\\?\Volume{DCBDB210-C414-409E-B108-C2BFA7395E1F}\",
                    DriveLetter = ""
                },
                new WMODrive { 
                    DeviceId = @"\\?\Volume{a0bf4e34-c90f-4005-815c-9a5a485e40ad}\",
                    DriveLetter = "D:"
                },
                new WMODrive {
                    DeviceId = @"\\?\Volume{ECBDB210-D414-509E-B108-C2BFA7395E1F}\",
                    DriveLetter = "C:"
                },
            });

            using (var subject = new PathNormaliser(fakeWMOProvider))
            {
                var normalisedPath = subject.Normalise(path);
                Assert.AreEqual(@"D:\Oculus\Software", normalisedPath);
            }

                
        }
    }
}