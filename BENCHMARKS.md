// * Summary *

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i7-8565U CPU 1.80GHz (Max: 2.00GHz) (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.111
  [Host]     : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3


| Method                  | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------------------ |---------:|--------:|--------:|-------:|----------:|
| EvaluateAsync_SingleKey | 160.1 ns | 1.40 ns | 1.24 ns | 0.0134 |      56 B |

// * Hints *
Outliers
  FixedWindowBenchmarks.EvaluateAsync_SingleKey: Default -> 1 outlier  was  removed (171.61 ns)

// * Legends *
  Mean      : Arithmetic mean of all measurements
  Error     : Half of 99.9% confidence interval
  StdDev    : Standard deviation of all measurements
  Gen0      : GC Generation 0 collects per 1000 operations
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 ns      : 1 Nanosecond (0.000000001 sec)

// * Diagnostic Output - MemoryDiagnoser *


// ***** BenchmarkRunner: End *****
Run time: 00:00:19 (19.28 sec), executed benchmarks: 1

Global total time: 00:00:35 (35.08 sec), executed benchmarks: 1
