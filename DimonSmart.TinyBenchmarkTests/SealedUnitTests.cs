using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

public class SealedUnitTests
{
    private readonly SealedSimple sealedClass = new SealedSimple();
    private readonly NonSealedSimple nonSealedClass = new NonSealedSimple();
    SealedSimple[] sealedTypeArray = new SealedSimple[100];
    NonSealedSimple[] nonSealedTypeArray = new NonSealedSimple[100];


    [TinyBenchmarkParameter(1,2,3,4,5)]
    public int BenchmarkParameter { get; set; }

   // [TinyBenchmark]
   // public void NonSealedClassWithParameterTest(int parameter) => _ = nonSealedClass.GetNumber(parameter);
   //
   // [TinyBenchmark]
   // public void SealedClassWithParameterTest(int parameter) => _ = sealedClass.GetNumber(parameter);
   //
   // [TinyBenchmark]
   // public bool Is_Sealed(int parameter) => sealedClass is SealedSimple;
   //
   // [TinyBenchmark]
   // public bool Is_NotSealed(int parameter) => nonSealedClass is NonSealedSimple;
   //
   [TinyBenchmark]
   public void NonSealedArrayAcces(int parameter) => nonSealedTypeArray[0] = new NonSealedSimple();

    [TinyBenchmark]
    public void SealedArrayAcces() => sealedTypeArray[0] = new SealedSimple();
}
