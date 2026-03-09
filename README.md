# **FastSearch**
## _Lightning-fast string searching for your .NET applications!_

✨ **Features**
- **Effortless Searching**: Quickly perform case-insensitive searches across a large list of objects.
- **Multiple Search Algorithms**: Choose from various algorithms:
  - **LinqSearch**: Utilizes Parallel LINQ for efficient searching.
  - **HashSearch**: Leverages precomputed hashes for rapid substring searches.
  - **CharSequenceSearch**: Implements a character sequence tree for advanced searching capabilities.
- **Customizable Object Representation**: Easily define how your objects are represented in searches by overriding `ToString()` or providing a custom index function.

🚀 **Quick Start**
1. Download the **FastSearch** library from NuGet.
2. Ensure your objects implement `override ToString()`.
3. Create a new instance of `CharSequenceSearch` with your `IEnumerable<T>`.

### **Performance Insights**
- FastSearch is designed for high performance, capable of handling thousands of searches in minimal time.
- Indexing and searching have been optimized for speed, making it suitable for large datasets.

### **Why Choose FastSearch?**
- **User-Friendly**: Designed with developers in mind, enabling you to integrate powerful search functionality with ease.
- **Future-Proof**: Regular updates and improvements ensure you have access to the latest searching techniques.

Enjoy the power of efficient searching with FastSearch!

---

### **Contributing**
We welcome contributions! Please feel free to submit issues or pull requests.

### **License**
This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE) file for more details.

---

<sub>Powered by [RepoBeacon](https://repobeacon.com)</sub>
