# FastSearch

Welcome to FastSearch!

This project is meant to provide quick, easy text (i.e. string) searching over a list of objects. We take two assumptions which is that we are doing case insenstive searching and can search at any point in the string.

|Algorithm |Description |
--- | --- |
|LinqSearch|Uses a simple Parallel LINQ Contains query
BetterLinqSearch |Uses a simple Parallel LINQ Contains query with precomputed case insensitive strings |
| MapReduceSearch |Uses a Map Reduce technique (Not Recommended)|
| HashSearch | Uses a precomputed hash of each substring combination |
| CharSequenceSearch | Uses a character sequence tree to faciliate searching |

We will update the library with new and more interesting techniques as time goes on.  In addition, we plan to keep this a generic library which can be used widely by all developers.  We are also doing this to deprecate functionality easily as new and improved algorithms are made available.

If you have very large lists and index speed does not matter, then use CharSequenceSearch. This will provide very low nanosecond searches. In
Enjoy!

[Your Friends @MILL5](https://www.mill5.com)