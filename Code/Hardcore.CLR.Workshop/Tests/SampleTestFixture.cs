using ILRewriter;
using ILRewriter.Extensions;
using NUnit.Framework;
using SampleLibrary;

namespace Tests
{
    [TestFixture]
    public class SampleTestFixture : BaseCecilTestFixture
    {

        [Test]
        public void Should_modify_console_writeline_string()
        {
            var modifiedAssembly = RewriteAssemblyOf<SampleClassWithInstanceMethod>();

            modifiedAssembly.Write(@"C:\Users\skwalocal\Source\Repos\Hardcore.CLR.Workshop\Code\Hardcore.CLR.Workshop\Tests\bin\Debug\testassembly.dll");

            var modifiedType = CreateModifiedType(modifiedAssembly, nameof(SampleClassWithInstanceMethod));

            
            // Call the DoSomething() method
            // with the modified Console.WriteLine call
            modifiedType.DoSomething();
            return;
        }
        protected override IAssemblyModifier GetAssemblyModifier()
        {
            return new SampleAssemblyModifier();
        }
    }
}