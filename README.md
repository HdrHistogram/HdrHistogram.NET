<img style="float: right;" align="right" height=64 src="https://raw.githubusercontent.com/HdrHistogram/HdrHistogram.NET/master/HdrHistogram-icon-64x64.png">

# HdrHistogram
**A High Dynamic Range (HDR) Histogram**

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/HdrHistogram/HdrHistogram?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Build status](https://ci.appveyor.com/api/projects/status/q0o5faahigq6u4qe/branch/master?svg=true)](https://ci.appveyor.com/project/LeeCampbell/hdrhistogram-net/branch/master) [![Build status](https://ci.appveyor.com/api/projects/status/q0o5faahigq6u4qe?svg=true)](https://ci.appveyor.com/project/LeeCampbell/hdrhistogram-net)

## What is it
HdrHistogram.NET is the official port of the Java HdrHistogram library.
All official implementations of HdrHistogram can be found at https://github.com/HdrHistogram

## Why would I use it?
You would use it to efficiently capture large number of response times measurements.

Often when measuring response times, one could make the common mistake of reporting on the mean value or the 90th percentile.
Gil Tene (the original author of the Java HdrHistogram) illustrates in numerous presentations (such as [here](http://www.infoq.com/presentations/latency-pitfalls) and [here](https://www.youtube.com/watch?v=9MKY4KypBzg)) on why this is a mistake.
Instead you want to collect all of the data and then be able to report your measurements across the range of measurements.

## How would I use it?
The library is available as a package from Nuget as [HdrHistogram](https://www.nuget.org/packages/HdrHistogram/)

Generally you want to be able to record at the finest accuracy the response-time of a given function of your software. 
To do this code might look something like this

### Declare the Histogram

``` csharp
// A Histogram covering the range from ~466 nanoseconds to 1 hour (3,600,000,000,000 ns) with a resolution of 3 significant figures:
var histogram = new LongHistogram(TimeStamp.Hours(1), 3);

```

### Record your measurements
Next you would record your measurements.
The `System.Diagnostics.Stopwatch.GetTimestamp()` method provides the most accurate way to record the elapsed time an action took to run.
By measuring the difference of the timestamp values before and after the action to measure, we can get the most accurate recording of elapsed
time available on the .NET platform.

```  csharp
long startTimestamp = Stopwatch.GetTimestamp();
//Execute some action to be measured
long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
histogram.RecordValue(elapsed);

```

### Output the results.
Once you have recorded all of your data, you are able to present that data based on a highly dynamic range of buckets.
We are not interested in all the values, but just enough of the values to get a picture of our system's performance.
To do this we want to generate a percentile distribution, with exponentially increasing fidelity.

Here we show an example of writing to the `Console`.

```  csharp
var writer = new StringWriter();
var scalingRatio = OutputScalingFactor.TimeStampToMicroseconds;
histogram.OutputPercentileDistribution(
  writer,
  outputValueUnitScalingRatio: scalingRatio);
Console.WriteLine(writer.ToString());
//Or just simply write directly to the Console output  stream
//histogram.OutputPercentileDistribution(
//  Console.Out,
//  outputValueUnitScalingRatio: scalingRatio);
```

Would produce output similar to:

```
       Value     Percentile TotalCount 1/(1-Percentile)

       0.285 0.000000000000          1           1.00
       0.448 0.100000000000       3535           1.11
       0.466 0.200000000000       7100           1.25
       0.497 0.300000000000      10504           1.43
       0.523 0.400000000000      14046           1.67
       0.535 0.500000000000      17644           2.00
       0.541 0.550000000000      19466           2.22
       0.547 0.600000000000      21134           2.50
       0.555 0.650000000000      22898           2.86
       0.567 0.700000000000      24513           3.33
       0.594 0.750000000000      26260           4.00
       0.609 0.775000000000      27129           4.44
       0.627 0.800000000000      28005           5.00
       0.642 0.825000000000      28939           5.71
       0.660 0.850000000000      29793           6.67
       0.680 0.875000000000      30649           8.00
       0.687 0.887500000000      31095           8.89
       0.693 0.900000000000      31550          10.00
       0.698 0.912500000000      31992          11.43
       0.703 0.925000000000      32415          13.33
       0.710 0.937500000000      32880          16.00
       0.713 0.943750000000      33080          17.78
       0.717 0.950000000000      33277          20.00
       0.721 0.956250000000      33476          22.86
       0.727 0.962500000000      33710          26.67
       0.736 0.968750000000      33925          32.00
       0.741 0.971875000000      34023          35.56
       0.748 0.975000000000      34141          40.00
       0.757 0.978125000000      34249          45.71
       0.768 0.981250000000      34352          53.33
       0.786 0.984375000000      34459          64.00
       0.803 0.985937500000      34515          71.11
       0.815 0.987500000000      34567          80.00
       0.838 0.989062500000      34622          91.43
       0.869 0.990625000000      34676         106.67
       1.045 0.992187500000      34731         128.00
       1.815 0.992968750000      34759         142.22
       1.943 0.993750000000      34786         160.00
       1.989 0.994531250000      34813         182.86
       2.038 0.995312500000      34841         213.33
       2.087 0.996093750000      34868         256.00
       2.127 0.996484375000      34881         284.44
       2.161 0.996875000000      34895         320.00
       2.225 0.997265625000      34909         365.71
       2.355 0.997656250000      34922         426.67
       2.539 0.998046875000      34936         512.00
       2.601 0.998242187500      34943         568.89
       2.653 0.998437500000      34950         640.00
       2.689 0.998632812500      34957         731.43
       2.755 0.998828125000      34964         853.33
       2.801 0.999023437500      34970        1024.00
       2.827 0.999121093750      34974        1137.78
       2.847 0.999218750000      34977        1280.00
       2.889 0.999316406250      34982        1462.86
       2.947 0.999414062500      34984        1706.67
       2.979 0.999511718750      34987        2048.00
       3.015 0.999560546875      34989        2275.56
       3.131 0.999609375000      34991        2560.00
       3.267 0.999658203125      34993        2925.71
       3.397 0.999707031250      34994        3413.33
       3.627 0.999755859375      34996        4096.00
       3.845 0.999780273438      34997        4551.11
       3.995 0.999804687500      34998        5120.00
       4.299 0.999829101563      34999        5851.43
       4.299 0.999853515625      34999        6826.67
       4.839 0.999877929688      35000        8192.00
      10.039 0.999890136719      35001        9102.22
      10.039 0.999902343750      35001       10240.00
      11.911 0.999914550781      35002       11702.86
      11.911 0.999926757813      35002       13653.33
      11.911 0.999938964844      35002       16384.00
      15.367 0.999945068359      35003       18204.44
      15.367 0.999951171875      35003       20480.00
      15.367 0.999957275391      35003       23405.71
      15.367 0.999963378906      35003       27306.67
      15.367 0.999969482422      35003       32768.00
    2543.615 0.999972534180      35004       36408.89
    2543.615 1.000000000000      35004
#[Mean    =        0.633, StdDeviation   =       13.588]
#[Max     =     2541.568, Total count    =        35004]
#[Buckets =           21, SubBuckets     =         2048]
```
Note that in the example above a value for the optional parameter `outputValueUnitScalingRatio` is provided.
If you record elapsed time using the suggested method with `Stopwatch.GetTimestamp()`, then you will have recorded values in a non-standard unit of time.
Instead of paying to cost of converting recorded values at the time of recording, record raw values.
Use the helper methods to convert recorded values to standard units at output time, when performance is less critical.


### Example of reporting results as a chart
You can also have HdrHistogram output the results in a file format that can be charted.
This is especially useful when comparing measurements.

First you will need to create the file to be used as an input for the chart.

```  csharp
using (var writer = new StreamWriter("HistogramResults.hgrm"))
{
	histogram.OutputPercentileDistribution(writer);
}
```

The data can then be plotter to visualize the percentile distribution of your results.
Multiple files can be plotted in the same chart allowing effective visual comparison of your results.
You can use either

 * the online tool - http://hdrhistogram.github.io/HdrHistogram/plotFiles.html
 * the local tool - _.\GoogleChartsExample\plotFiles.html_
  ![](http://i.imgur.com/Z1wIqw1.png)

If you use the local tool, there are example result files in the _.\GoogleChartsExample_ directory.
The tool also allows you to export to png.

## So what is so special about this way of recording response times?

* itself is low latency
* tiny foot print due to just storing a dynamic range of buckets and counts
* produces the reports you actually want

## Full code example
This code sample show a recording of the time taken to execute a ping request.
We execute and record this in a loop.

```  csharp
// A Histogram covering the range from ~466 nanoseconds to 1 hour (3,600,000,000,000 ns) with a resolution of 3 significant figures:
var histogram = new LongHistogram(TimeStamp.Hours(1), 3);
using (var ping = new System.Net.NetworkInformation.Ping())
{
	for (int i = 0; i < 100; i++)
	{
		long startTimestamp = Stopwatch.GetTimestamp();
		//Execute our action we want to record.
		ping.Send("www.github.com");
		long elapsed = Stopwatch.GetTimestamp() - startTimestamp;
		histogram.RecordValue(elapsed);
	}
}
//Output the percentile distribution of our results to the Console with values presented in Milliseconds
histogram.OutputPercentileDistribution(
	printStream: Console.Out,
	percentileTicksPerHalfDistance: 3,
	outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds);

```

**output:**

```
       Value     Percentile TotalCount 1/(1-Percentile)

      79.360 0.000000000000          1           1.00
      80.435 0.166666666667         17           1.20
      80.896 0.333333333333         36           1.50
      81.050 0.500000000000         52           2.00
      81.152 0.583333333333         59           2.40
      81.254 0.666666666667         70           3.00
      81.357 0.750000000000         76           4.00
      81.459 0.791666666667         86           4.80
      81.459 0.833333333333         86           6.00
      81.510 0.875000000000         93           8.00
      81.510 0.895833333333         93           9.60
      81.510 0.916666666667         93          12.00
      81.562 0.937500000000         94          16.00
      81.613 0.947916666667         98          19.20
      81.613 0.958333333333         98          24.00
      81.613 0.968750000000         98          32.00
      81.613 0.973958333333         98          38.40
      81.613 0.979166666667         98          48.00
      81.664 0.984375000000         99          64.00
      81.664 0.986979166667         99          76.80
      81.664 0.989583333333         99          96.00
      86.067 0.992187500000        100         128.00
      86.067 1.000000000000        100
#[Mean    =       80.964, StdDeviation   =        0.746]
#[Max     =       86.067, Total count    =          100]
#[Buckets =           26, SubBuckets     =         2048]
```



### How would I contribute to this project?
We welcome pull requests!
If you do choose to contribute, please first raise an issue so we are not caught off guard by the pull request.
Next can you please ensure that your PR (Pull Request) has a comment in it describing what it achieves and the issues that it closes.
Ideally if it is fixing an issue or a bug, there would be a Unit Test proving the fix and a reference to the Issues in the PR comments.


### How to run tests?
If you are having trouble running xunit tests out of the box, it is possible to run them using a version of the command line below. 
You'll just need to fill in the correct value of $(SolutionDir), which should be as defined in Visual Studio.
PS> dotnet C:\Users\%USERNAME%\.nuget\packages\xunit.runner.console\2.3.1\tools\netcoreapp2.0\xunit.console.dll $(SolutionDir)\HdrHistogram.UnitTests\bin\Debug\netcoreapp1.1\HdrHistogram.UnitTests.dll


HdrHistogram Details
----------------------------------------------

An HdrHistogram supports the recording and analyzing of sampled data value counts across a configurable integer value range with configurable value precision within the range.
Value precision is expressed as the number of significant digits in the value recording, and provides control over value quantization behavior across the value range and the subsequent value resolution at any given level.

For example, a Histogram could be configured to track the counts of observed integer values between 0 and 3,600,000,000 while maintaining a value precision of 3 significant digits across that range.
Value quantization within the range will thus be no larger than 1/1,000th (or 0.1%) of any value.
This example Histogram could be used to track and analyze the counts of observed response times ranging between 1 microsecond and 1 hour in magnitude.
This Histogram would still maintain a value resolution of 1 microsecond up to 1 millisecond, a resolution of 1 millisecond (or better) up to one second, and a resolution of 1 second (or better) up to 1,000 seconds.
At its maximum tracked value (1 hour), it would still maintain a resolution of 3.6 seconds (or better).

The HdrHistogram package includes the `LongHistogram` implementation, which tracks value counts in `long` fields, and is expected to be the commonly used Histogram form.
`IntHistogram` and `ShortHistogram`, which track value counts in `int` and `short` fields respectively, are provided for use cases where smaller count ranges are practical and smaller overall storage is beneficial.
Performance impacts should be measured prior to choosing one over the other in the name of optimization.

HdrHistogram is designed for recoding histograms of value measurements in latency and performance sensitive applications.
Measurements show value recording times as low as 3-6 nanoseconds on modern (circa 2012) Intel CPUs.
That is, 1,000,000,000 (1 billion) recordings can be made at a total cost of around 3 seconds on modern hardware.
A Histogram's memory footprint is constant, with no allocation operations involved in recording data values or in iterating through them.
The memory footprint is fixed regardless of the number of data value samples recorded, and depends solely on the dynamic range and precision chosen.
The amount of work involved in recording a sample is constant, and directly computes storage index locations such that no iteration or searching is ever involved in recording data values.

A combination of high dynamic range and precision is useful for collection and accurate post-recording analysis of sampled value data distribution in various forms.
Whether it's calculating or plotting arbitrary percentiles, iterating through and summarizing values in various ways, or deriving mean and standard deviation values, the fact that the recorded data information is kept in high resolution allows for accurate post-recording analysis with low [and ultimately configurable] loss in accuracy when compared to performing the same analysis directly on the potentially infinite series of sourced data values samples.

An common use example of HdrHistogram would be to record response times in units of microseconds across a dynamic range stretching from 1 usec to over an hour, with a good enough resolution to support later performing post-recording analysis on the collected data.
Analysis can include computing, examining, and reporting of distribution by percentiles, linear or logarithmic value buckets, mean and standard deviation, or by any other means that can can be easily added by using the various iteration techniques supported by the Histogram.
In order to facilitate the accuracy needed for various post-recording analysis techniques, this example can maintain a resolution of ~1 usec or better for times ranging to ~2 msec in magnitude, while at the same time maintaining a resolution of ~1 msec or better for times ranging to ~2 sec, and a resolution of ~1 second or better for values up to 2,000 seconds.
This sort of example resolution can be thought of as "always accurate to 3 decimal points."
Such an example Histogram would simply be created with a highestTrackableValue of 3,600,000,000, and a numberOfSignificantValueDigits of 3, and would occupy a fixed, unchanging memory footprint of around 185KB (see "Footprint estimation" below).


Histogram variants and internal representation
----------------------------------------------

The HdrHistogram package includes multiple implementations of the
`HistogramBase` class:
- `LongHistogram`, which is the commonly used Histogram form and tracks value counts in `long` fields.
- `IntHistogram` and `ShortHistogram`, which track value counts in `int` and `short` fields respectively, are provided for use cases where smaller count ranges are practical and smaller overall storage is beneficial (e.g. systems where tens of thousands of in-memory histogram are being tracked).
- `SynchronizedHistogram` (see 'Synchronization and concurrent access' below)

Internally, data in HdrHistogram variants is maintained using a concept somewhat similar to that of floating point number representation.
Using an exponent a (non-normalized) mantissa to support a wide dynamic range at a high but varying (by exponent value) resolution.
Histograms use exponentially increasing bucket value ranges (the parallel of the exponent portion of a floating point number) with each bucket containing a fixed number (per bucket) set of linear sub-buckets (the parallel of a non-normalized mantissa portion of a floating point number).
Both dynamic range and resolution are configurable, with `highestTrackableValue` controlling dynamic range, and `numberOfSignificantValueDigits` controlling resolution.

Synchronization and concurrent access
----------------------------------------------

In the interest of keeping value recording cost to a minimum, the commonly used `LongHistogram` class and its `IntHistogram` and `ShortHistogram` variants are NOT internally synchronized, and do NOT use atomic variables.
Callers wishing to make potentially concurrent, multi-threaded updates or queries against Histogram objects should either take care to externally synchronize and/or order their access, or use the `SynchronizedHistogram` variant.
It is worth mentioning that since Histogram objects are additive, it is common practice to use per-thread, non-synchronized histograms for the recording fast path, and "flipping" the actively recorded-to histogram (usually with some non-locking variants on the fast path) and having a summary/reporting thread perform histogram aggregation math across time and/or threads.

Iteration
----------------------------------------------

Histograms supports multiple convenient forms of iterating through the histogram data set, including linear, logarithmic, and percentile iteration mechanisms, as well as means for iterating through each recorded value or each possible value level.
The iteration mechanisms are accessible through the HistogramData available through `getHistogramData()`.
Iteration mechanisms all provide `HistogramIterationValue` data points along the histogram's iterated data set.

Recorded values are available as instance methods:

 - `RecordedValues`: An `IEnumerable<HistogramIterationValue>` through the histogram using a `RecordedValuesEnumerable`\`RecordedValuesEnumerator`
 - `AllValues`: An `IEnumerable<HistogramIterationValue>` through the histogram using a `AllValueEnumerable`\`AllValuesEnumerator`

All others are available for the default (corrected) histogram data set via the following extension methods:

 - `Percentiles`: An `IEnumerable<HistogramIterationValue>` through the histogram using a `PercentileEnumerable`/`PercentileEnumerator`
 - `LinearBucketValues`: An `IEnumerable<HistogramIterationValue>` through the histogram using a `LinearBucketEnumerable`/`LinearEnumerator`
 - `LogarithmicBucketValues`: An `IEnumerable<HistogramIterationValue>` through the histogram using a `LogarithmicBucketEnumerable`/`LogarithmicEnumerator`



Iteration is typically done with a for-each loop statement. E.g.:

``` csharp
 foreach (var v in histogram.Percentiles(ticksPerHalfDistance))
 {
     ...
 }
```

 or

``` csharp
 for (var v in histogram.LinearBucketValues(unitsPerBucket))
 {
     ...
 }
```

These enumerators are optimised for fast forward readonly "_hosepipe_" usage.
They are low allocation and may reuse objects internally to keep allocations low and thus reduce garbage collection/memory pressure.

Equivalent Values and value ranges
----------------------------------------------

Due to the finite (and configurable) resolution of the histogram, multiple adjacent integer data values can be "equivalent".
Two values are considered "equivalent" if samples recorded for both are always counted in a common total count due to the histogram's resolution level.
HdrHistogram provides methods for

 * determining the lowest and highest equivalent values for any given value,
 * determining whether two values are equivalent,
 * finding the next non-equivalent value for a given value (useful when looping through values, in order to avoid a double-counting count).

Corrected vs. Raw value recording calls
----------------------------------------------

In order to support a common use case needed when histogram values are used to track response time distribution, Histogram provides for the recording of corrected histogram value by supporting a `RecordValueWithExpectedInterval(long, long)` variant is provided.
This value recording form is useful in [common latency measurement] scenarios where response times may exceed the expected interval between issuing requests, leading to "dropped" response time measurements that would typically correlate with "bad" results.

When a value recorded in the histogram exceeds the `expectedIntervalBetweenValueSamples` parameter, recorded histogram data will reflect an appropriate number of additional values, linearly decreasing in steps of `expectedIntervalBetweenValueSamples`, down to the last value that would still be higher than `expectedIntervalBetweenValueSamples`.

To illustrate why this corrective behavior is critically needed in order to accurately represent value distribution when large value measurements may lead to missed samples, imagine a system for which response times samples are taken once every 10 msec to characterize response time distribution.
The hypothetical system behaves "perfectly" for 100 seconds (10,000 recorded samples), with each sample showing a 1msec response time value.
At each sample for 100 seconds (10,000 logged samples at 1 msec each).
The hypothetical system then encounters a 100 sec pause during which only a single sample is recorded (with a 100 second value).
The raw data histogram collected for such a hypothetical system (over the 200 second scenario above) would show ~99.99% of results at 1 msec or below, which is obviously "not right".
The same histogram, corrected with the knowledge of an `expectedIntervalBetweenValueSamples` of 10msec will correctly represent the response time distribution.
Only ~50% of results will be at 1 msec or below, with the remaining 50% coming from the auto-generated value records covering the missing increments spread between 10msec and 100 sec.

Data sets recorded with and without an `expectedIntervalBetweenValueSamples` parameter will differ only if at least one value recorded with the `RecordValue(..)` method was greater than its associated `expectedIntervalBetweenValueSamples` parameter.
Data sets recorded with an `expectedIntervalBetweenValueSamples` parameter will be identical to ones recorded without it if all values recorded via the `RecordValue(..)` calls were smaller than their associated (and optional) `expectedIntervalBetweenValueSamples` parameters.

When used for response time characterization, the recording with the optional `expectedIntervalBetweenValueSamples` parameter will tend to produce data sets that would much more accurately reflect the response time distribution that a random, uncoordinated request would have experienced.

Footprint estimation
----------------------------------------------

Due to it's dynamic range representation, Histogram is relatively efficient in memory space requirements given the accuracy and dynamic range it covers.
Still, it is useful to be able to estimate the memory footprint involved for a given `highestTrackableValue` and `numberOfSignificantValueDigits` combination.
Beyond a relatively small fixed-size footprint used for internal fields and stats (which can be estimated as "fixed at well less than 1KB"), the bulk of a Histogram's storage is taken up by it's data value recording counts array.
The total footprint can be conservatively estimated by:

```  csharp
 largestValueWithSingleUnitResolution = 2 * (10 ^ numberOfSignificantValueDigits);
 subBucketSize = RoundedUpToNearestPowerOf2(largestValueWithSingleUnitResolution);

 expectedHistogramFootprintInBytes = 512 +
      ({primitive type size} / 2) *
      (Log2RoundedUp((highestTrackableValue) / subBucketSize) + 2) *
      subBucketSize;
```

A conservative (high) estimate of a Histogram's footprint in bytes is available via the `GetEstimatedFootprintInBytes()` method.

## Terminology

  * **Latency** : The time that something is latent i.e. not being processed.
 This maybe due to being in a queue.
  * **Service Time** : The time taken to actually service a request.
  * **Response time** : The sum of the latency and the service time. e.g. the time your request was queued, plus the time it took to process.

References (see also)
 - [How NOT to Measure Latency](http://www.infoq.com/presentations/latency-pitfalls) Gil Tene - qCon 2013
 - [Understanding Latency](https://www.youtube.com/watch?v=9MKY4KypBzg)  Gil Tene - React San Francisco 2014
 - [Designing for Performance](https://youtu.be/fDGWWpHlzvw?t=4m56s) Martin Thompson - GOTO Chicago 2015
 - https://en.wikipedia.org/wiki/Response_time_(technology)
