
# Benchmarks

## Benchmark (55 items)

|                                  Method | Runtime |  Toolchain |       Mean | Error | Scaled |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|----------------------------------------- |-------- |----------- |-----------:|------:|-------:|----------:|----------:|----------:|------------:|
| ConcurrentDirectoryUploadPartsize16CpuX1 |     Clr |    Default | 2,852.9 ms |    NA |   1.00 | 5187.5000 | 5187.5000 | 5187.5000 | 25236.25 KB |
| ConcurrentDirectoryUploadPartsize16CpuX2 |     Clr |    Default | 3,011.1 ms |    NA |   1.06 | 5187.5000 | 5187.5000 | 5187.5000 | 25236.75 KB |
|  ConcurrentDirectoryUploadPartsize5CpuX1 |     Clr |    Default | 2,704.7 ms |    NA |   0.95 | 5187.5000 | 5187.5000 | 5187.5000 | 25232.25 KB |
|       ConcurretFileUploadPartsize16CpuX1 |     Clr |    Default |   454.4 ms |    NA |   0.16 | 4437.5000 | 3625.0000 | 2937.5000 | 25509.56 KB |
|       ConcurretFileUploadPartsize16CpuX2 |     Clr |    Default |   461.0 ms |    NA |   0.16 | 4687.5000 | 3750.0000 | 3062.5000 | 25526.78 KB |
|        ConcurretFileUploadPartsize5CpuX1 |     Clr |    Default |   359.8 ms |    NA |   0.13 | 4625.0000 | 3750.0000 | 3062.5000 | 25617.95 KB |
| ConcurrentDirectoryUploadPartsize16CpuX1 |    Core | CoreCsProj | 3,446.6 ms |    NA |   1.00 | 5187.5000 | 5187.5000 | 5187.5000 |     1.05 KB |
| ConcurrentDirectoryUploadPartsize16CpuX2 |    Core | CoreCsProj | 3,521.6 ms |    NA |   1.02 | 5187.5000 | 5187.5000 | 5187.5000 |     1.05 KB |
|  ConcurrentDirectoryUploadPartsize5CpuX1 |    Core | CoreCsProj | 2,773.9 ms |    NA |   0.80 | 5187.5000 | 5187.5000 | 5187.5000 |     1.05 KB |
|       ConcurretFileUploadPartsize16CpuX1 |    Core | CoreCsProj |   409.0 ms |    NA |   0.12 | 2812.5000 | 2125.0000 | 1562.5000 | 20085.78 KB |
|       ConcurretFileUploadPartsize16CpuX2 |    Core | CoreCsProj |   411.3 ms |    NA |   0.12 | 3000.0000 | 2375.0000 | 1750.0000 | 20086.08 KB |
|        ConcurretFileUploadPartsize5CpuX1 |    Core | CoreCsProj |   256.4 ms |    NA |   0.07 | 2875.0000 | 2312.5000 | 1687.5000 | 20085.62 KB |


## Benchmark 2 (55 items)


|                            Method | Runtime |  Toolchain |     Mean | Error | Scaled |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|----------------------------------- |-------- |----------- |---------:|------:|-------:|----------:|----------:|----------:|----------:|
| ConcurretFileUploadPartsize16CpuX1 |     Clr |    Default | 198.8 ms |    NA |   1.00 | 4375.0000 | 3687.5000 | 2750.0000 |  24.94 MB |
| ConcurretFileUploadPartsize16CpuX2 |     Clr |    Default | 241.5 ms |    NA |   1.21 | 4250.0000 | 3500.0000 | 2687.5000 |  24.89 MB |
|  ConcurretFileUploadPartsize5CpuX1 |     Clr |    Default | 240.4 ms |    NA |   1.21 | 4437.5000 | 3500.0000 | 2750.0000 |  24.83 MB |
| ConcurretFileUploadPartsize16CpuX1 |    Core | CoreCsProj | 141.5 ms |    NA |   1.00 | 3375.0000 | 2812.5000 | 2125.0000 |  19.62 MB |
| ConcurretFileUploadPartsize16CpuX2 |    Core | CoreCsProj | 225.3 ms |    NA |   1.59 | 2812.5000 | 2250.0000 | 1625.0000 |  19.62 MB |
|  ConcurretFileUploadPartsize5CpuX1 |    Core | CoreCsProj | 119.3 ms |    NA |   0.84 | 3312.5000 | 2687.5000 | 2125.0000 |  19.62 MB |


## Benchmark3 (2922 items)

|                            Method | Runtime |  Toolchain |    Mean | Error | Scaled |       Gen 0 |       Gen 1 |      Gen 2 |  Allocated |
|----------------------------------- |-------- |----------- |--------:|------:|-------:|------------:|------------:|-----------:|-----------:|
| ConcurretFileUploadPartsize16CpuX1 |     Clr |    Default | 3.293 s |    NA |   1.00 | 153125.0000 |  89437.5000 | 76375.0000 |  1124.4 MB |
| ConcurretFileUploadPartsize16CpuX2 |     Clr |    Default | 3.792 s |    NA |   1.15 | 162312.5000 |  92250.0000 | 79312.5000 |  1125.6 MB |
|  ConcurretFileUploadPartsize5CpuX1 |     Clr |    Default | 3.751 s |    NA |   1.14 | 165875.0000 | 102312.5000 | 90312.5000 | 1128.92 MB |
| ConcurretFileUploadPartsize16CpuX1 |    Core | CoreCsProj | 9.403 s |    NA |   1.00 | 134250.0000 | 105937.5000 | 82625.0000 |  908.25 MB |
| ConcurretFileUploadPartsize16CpuX2 |    Core | CoreCsProj | 5.404 s |    NA |   0.57 | 151687.5000 |  88500.0000 | 77375.0000 |  908.25 MB |
|  ConcurretFileUploadPartsize5CpuX1 |    Core | CoreCsProj | 5.638 s |    NA |   0.60 | 130437.5000 |  98500.0000 | 79250.0000 |  908.24 MB |


## Benchmark4 (5725 items)

|                            Method | Runtime |  Toolchain |    Mean | Error | Scaled |       Gen 0 |       Gen 1 |       Gen 2 | Allocated |
|----------------------------------- |-------- |----------- |--------:|------:|-------:|------------:|------------:|------------:|----------:|
| ConcurretFileUploadPartsize16CpuX1 |     Clr |    Default | 6.565 s |    NA |   1.00 | 268250.0000 | 144687.5000 | 115625.0000 |   2.16 GB |
| ConcurretFileUploadPartsize16CpuX2 |     Clr |    Default | 5.666 s |    NA |   0.86 | 265187.5000 | 141875.0000 | 113437.5000 |   2.15 GB |
|  ConcurretFileUploadPartsize5CpuX1 |     Clr |    Default | 5.990 s |    NA |   0.91 | 276125.0000 | 145437.5000 | 116875.0000 |   2.16 GB |
| ConcurretFileUploadPartsize16CpuX1 |    Core | CoreCsProj | 3.448 s |    NA |   1.00 | 296312.5000 | 257125.0000 | 194500.0000 |   1.74 GB |
| ConcurretFileUploadPartsize16CpuX2 |    Core | CoreCsProj | 3.436 s |    NA |   1.00 | 285125.0000 | 243875.0000 | 184062.5000 |   1.74 GB |
|  ConcurretFileUploadPartsize5CpuX1 |    Core | CoreCsProj | 3.535 s |    NA |   1.03 | 289125.0000 | 247562.5000 | 187687.5000 |   1.74 GB |