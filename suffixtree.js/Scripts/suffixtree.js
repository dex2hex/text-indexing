/////////////////////////////////////////////////////////////////////
//      File Name          : suffixtree.js
//      Created            : 22 9 2013   20:20
//      Author             : Costin S
//
/////////////////////////////////////////////////////////////////////

node = function () {
    this.children = {};
    this.edge = null;
    this.link = null;
    this.parent = null;
    this.isleaf = false;
    this.leafnumber = undefined;

    this.hasedge = function (start) {
        return this.children[start] != undefined;
    }

    this.getedge = function (start) {
        if (this.children[start] != undefined)
            return this.children[start];
        else
            return null;
    }

    this.addedge = function (text, start, end) {
        if (end == -1 || (0 <= start && start <= end && end < text.length) || this.children[text[start]] == undefined) {
            var child = new node();
            child.edge = new edgelabel(start, end);
            child.parent = this;
            child.link = null;
            child.isleaf = false;

            this.children[text[start]] = child;
            return child;
        }
        else {
            throw "argument exception";
        }
    }

    this.removeedge = function (start) {
        var index = this.children[start];
        if (index != undefined) {
            var child = this.children[index];
            delete this.children[index];
            return child;
        }
        else {
            return null;
        }
    }

    this.setleaf = function (number) {
        this.isleaf = true;
        this.leafnumber = number;
    }
};

edgelabel = function(s, e){
	if (s < 0 || e < -1 || (e != -1 && s > e)){
		throw "argument exception";
	}
	this.start = s;
	this.end = e;
}

suffixtree = function () {
    this.text = null;
    this.phase = null;
    this.root = new node();

    this.charfromindex = function (i) {
        if (0 <= i && i < this.text.length) {
            return this.text[i];
        }
        throw "argument exception";
    }

    this.getsize = function (obj) {
        var size = 0, key;
        for (key in obj) {
            if (obj.hasOwnProperty(key)) size++;
        }
        return size;
    }

    this.edgelength = function (e) {
        return (e.end != -1 ? e.end : this.phase - 1) - e.start + 1;
    }

    this.traverseedge = function (p1, ms) {
        var index = this.charfromindex(ms);
        if (p1.children[index] == undefined) {
            throw "unexpected!";
        }

        return p1.children[index];
    }

    this.pre_match = function (p) {
        var result = {
            goDownFromRoot: true,
            target: null,
            edges: null
        };

        if (p == this.root) {
            throw "argument exception";
        }

        if (p.link != null) {
            result.target = p.link;
            result.edges = null;
            result.goDownFromRoot = false;

            return result;
        }

        var v = p.parent;

        if (v != this.root) {
            if (v.link == null) {

                var w = v.parent;
                if (w != this.root) {
                    if (w.link == null) {
                        throw new "unexpected";
                    }

                    result.edges = new Array();
                    result.edges[0] = new edgelabel(v.edge.start, v.edge.end == -1 ? (this.phase - 1) : v.edge.end);
                    result.edges[1] = new edgelabel(p.edge.start, p.edge.end == -1 ? (this.phase - 1) : p.edge.end);

                    result.target = w.link;
                    result.goDownFromRoot = false;
                }
                else {
                    if (v.link == null) {
                        throw new "unexpected";
                    }
                    result.edges = new Array();
                    result.edges[0] = new edgelabel(p.edge.start, p.edge.end == -1 ? (this.phase - 1) : p.edge.end);
                    result.target = v.link;
                    result.goDownFromRoot = false;
                }
            }
        }

        return result;
    }

    this.match = function (node, edges) {
        var result = {
            matchEndedAtNode: undefined,
            found: undefined,
            edgeCursor: undefined
        };

        if (edges == null || edges.length <= 0) {
            throw "argument exception";
        }

        var unmatchedEdgeCursor = -1;
        var matchingEdgeIndex = 0;
        var matchingEdgeCursor = edges[matchingEdgeIndex].start;
        var matchingEdgeEnd = edges[matchingEdgeIndex].end;

        var childNode = this.traverseedge(node, matchingEdgeCursor);
        var treeEdgeLength = this.edgelength(childNode.edge);

        do {
            var diff = matchingEdgeEnd - matchingEdgeCursor + 1 - treeEdgeLength;

            if (diff > 0) {
                matchingEdgeCursor += treeEdgeLength;
            }
            else {
                if (++matchingEdgeIndex < edges.length) {
                    matchingEdgeCursor = edges[matchingEdgeIndex].start;
                    matchingEdgeEnd = edges[matchingEdgeIndex].end;
                }
                else {
                    // nothing more to match .. 
                    if (diff < 0) //unmatched = -diff
                    {
                        unmatchedEdgeCursor = (childNode.edge.end == -1 ? (this.phase - 1) : childNode.edge.end) + diff + 1;
                    }
                    break;
                }
            }

            if (diff >= 0) {
                node = childNode;
                childNode = this.traverseedge(node, matchingEdgeCursor);
                treeEdgeLength = this.edgelength(childNode.edge);
            }
        } while (true);

        result.matchEndedAtNode = unmatchedEdgeCursor == -1;
        result.found = childNode;
        result.edgeCursor = unmatchedEdgeCursor;

        return result;
    }

    this.addtext = function (text) {
        this.text = text;

        var root = this.root;
        var m = this.text.length;

        var deep = root.addedge(this.text, 0, -1);
        deep.setleaf(0);

        var prevExtEnd = deep;
        var lastCreatedLeaf = -1;

        for (var i = 1; i < m; i++) {
            this.phase = i;

            var skipRemaining = false;
            var internCreatedPrevExt = null;

            var j = (lastCreatedLeaf == -1) ? 1 : lastCreatedLeaf;
            for (; j < i && !skipRemaining; j++) 
            {                
                var edges = null;
                var found = null;
                var edgeCursor = -1;
                var matchEndedAtNode;

                // select the right node before start matching...
                //
                if (j == lastCreatedLeaf && j > 1) {
                    if (!prevExtEnd.isleaf) {
                        edges = new Array();
                        edges[0] = new edgelabel(i - 1, i - 1);

                        var result = this.match(prevExtEnd, edges);
                        matchEndedAtNode = result.matchEndedAtNode;
                        found = result.found;
                        edgeCursor = result.edgeCursor;
                    }
                    else {
                        found = prevExtEnd;
                        matchEndedAtNode = true;
                    }
                }

                // now let's do some matching .. if still necessary that is...
                if (found == null) {
                    var result = this.pre_match(prevExtEnd);
                    var target = result.target;
                    edges = result.edges;

                    if (result.goDownFromRoot) {
                        edges = new Array();
                        edges[0] = new edgelabel(j, i - 1);

                        var tmpresult = this.match(this.root, edges);
                        matchEndedAtNode    = tmpresult.matchEndedAtNode;
                        found               = tmpresult.found;                                                        
                        edgeCursor          = tmpresult.edgeCursor;
                    }
                    else {
                        if (edges != null) {
                            var tmpresult = this.match(target, edges);
                            matchEndedAtNode    = tmpresult.matchEndedAtNode;
                            found               = tmpresult.found;
                            edgeCursor          = tmpresult.edgeCursor
                        }
                        else {
                            found = target;
                            matchEndedAtNode = true;
                        }
                    }
                }
                else {
                    matchEndedAtNode = true;
                }

                // matching has finished, time for some housekeeping
                if (!matchEndedAtNode) {
                    if (this.text[edgeCursor] == this.text[i]) {
                        skipRemaining = true;
                        break;
                    }
                    else {
                        var foundParent = found.parent;
                        var eStart = found.edge.start;
                        var eEnd = found.isleaf ? i - 1 : found.edge.end;
                        var eLength = eEnd - eStart + 1;

                        foundParent.removeedge(this.charfromindex(eStart));

                        var intern = new node();
                        intern.edge = new edgelabel(eStart, edgeCursor - 1);
                        intern.parent = foundParent;
                        intern.link = null;

                        foundParent.children[this.charfromindex(eStart)] = intern;

                        found.parent = intern;
                        found.edge.start = edgeCursor;

                        intern.children[this.charfromindex(found.edge.start)] = found;

                        if (internCreatedPrevExt != null) {
                            internCreatedPrevExt.link = intern;
                        }
                        internCreatedPrevExt = intern;

                        var newLeaf = intern.addedge(this.text, i, -1);
                        newLeaf.setleaf(j);
                        lastCreatedLeaf = j;
                        prevExtEnd = intern;
                    }
                }
                else {
                    if (found.isleaf) {
                        prevExtEnd = found;
                    }
                    else {
                        if (internCreatedPrevExt != null) {
                            internCreatedPrevExt.link = found;
                        }
                        internCreatedPrevExt = found.link == null ? found : null;

                        if (found.getedge(this.charfromindex(i)) == null) {
                            var newLeaf = found.addedge(this.text, i, -1);
                            newLeaf.setleaf(j);
                            lastCreatedLeaf = j;
                            prevExtEnd = found;
                        }
                        else {
                            skipRemaining = true;
                            break;
                        }
                    }
                }
            }

            if (!skipRemaining) {
                var parent = this.root;
                var child = parent.getedge(this.charfromindex(j));

                if (child == null) {
                    var newLeaf = parent.addedge(this.text, i, -1);
                    newLeaf.setleaf(j);
                    lastCreatedLeaf = j;
                    prevExtEnd = newLeaf;
                }

                if (internCreatedPrevExt != null) {
                    internCreatedPrevExt.link = parent;
                    internCreatedPrevExt = null;
                }
            }

        }

        this.fixupleaves(this.root, this.text.length - 1);
    }

    this.fixupleaves = function (p, endindex){
        if(p.isleaf) {
            p.edge.end = endindex;
        } else {
            var key;
            for(key in p.children){
                if (p.children.hasOwnProperty(key)) {
                    var child = p.children[key];
                    this.fixupleaves(child, endindex);
                }
            }
        }
    }    
}

