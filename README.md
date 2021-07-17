# FastSearch
## Welcome to FastSearch!

This project is meant to provide quick, easy text (i.e. string) searching over a list of objects. We take two assumptions which is that we are doing case insensitive searching and can search at any point in the string.

|Algorithm |Description |
--- | --- |
|LinqSearch|Uses a simple Parallel LINQ Contains query|
BetterLinqSearch |Uses a simple Parallel LINQ Contains query with precomputed case insensitive strings |
| MapReduceSearch |Uses a Map Reduce technique (Not Recommended)|
| HashSearch | Uses a precomputed hash of each substring combination |
| CharSequenceSearch | Uses a character sequence tree to facilitate searching |

Here are some quick results from testing.
![](https://raw.githubusercontent.com/MILL5/FastSearch/main/SearchResults.png)

We will update the library with new and more interesting techniques as time goes on.  In addition, we plan to keep this a generic library which can be used widely by all developers.  We are taking this approach replace functionality with new and improved algorithms as they are made available.

Enjoy!

[Your Friends @MILL5](https://www.mill5.com)