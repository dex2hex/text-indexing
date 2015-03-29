**Downloads v1.0**: can be found @ http://code.google.com/p/intervaltree/downloads/list.

**Downloads v1.+**: for latest version, browse source code directly!
Google has disabled the creation of downloads.


---

linear-time construction of
> - **suffix trees**
    * a compressed trie containing all the suffixes of the given text as their keys and positions in the text as their values (also called PAT tree or position tree).
    * implementation of Ukkonen's algorithm is given, a linear-time, online algorithm for constructing suffix trees
    * suffixtree.JS is provided along with a HTML visualization (d3js) for testing and experimentation (suffix links are not shown at the moment! will add later on)

> - **suffix arrays**
    * a sorted array of all suffixes of a string. It is a simple, yet powerful data structure which is used, among others, in full text indices, data compression algorithms and within the field of bioinformatics

> - **LCP\_array**
    * the longest common prefix array (LCP array) is an auxiliary data structure to the suffix array. It stores the lengths of the longest common prefixes between pairs of consecutive suffixes in the suffix array.

> - **Burrows-Wheeler Transform**
    * (also called block-sorting compression), is an algorithm used in data compression techniques such as bzip2.


**References:**

Ukkonen, E. (1995). "On-line construction of suffix trees". Algorithmica 14 (3): 249–260.



![https://text-indexing.googlecode.com/git/suffixtree.demo.png](https://text-indexing.googlecode.com/git/suffixtree.demo.png)