# FastSearch
## Welcome to FastSearch!

This project is meant to provide quick, easy text (i.e. string) searching over a list of objects. We take two assumptions which are that we are doing case insensitive searching and can search at any point in the string.

|Algorithm |Description |
--- | --- |
| LinqSearch |Uses a simple Parallel LINQ Contains query with precomputed case insensitive strings |
| HashSearch | Uses a precomputed hash of each substring combination |
| CharSequenceSearch | Uses a character sequence tree to facilitate searching |

Here are some quick results from testing. 
![](https://raw.githubusercontent.com/MILL5/FastSearch/main/SearchResults.png)

- Search results is the total time to perform 10000 searches.
- Indexing results is the total time to index 10000 items.

We will update the library with new and more interesting techniques as time goes on.  In addition, we plan to keep this a generic library which can be used widely by all developers.  We are taking this approach replace functionality with new and improved algorithms as they are made available.

### How To Get Started

1. Download the **FastSearch** library from NuGet.
2. Make sure your objects `override ToString()`.
3. Create a new CharSequenceSearch passing in your `IEnumerable<T>`

### Features

1. Support for `ToString()` out-of-the-box.
2. Index function `Func<T, string>` to return a custom string representation of an object.
3. Maximum degree of parallelism for index building and searching

Enjoy!

[Your Friends @MILL5](https://www.mill5.com)