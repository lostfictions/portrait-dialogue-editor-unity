'use strict';

var klay = require('klayjs');
var fs = require('fs');

var dialogue = JSON.parse(fs.readFileSync(process.argv[2]));

var width = 200;
var height = 200;

var graph = {
  "id": "root",
  "properties": {
      "direction": "RIGHT", "spacing": 40
  },
  "children": [],
  "edges": []
};

for(var q of dialogue.questions) {
	graph.children.push({
		"id": q.id.toString(),
		"width": width,
		"height": height
	})
	for(var ch of q.choices) {
		graph.edges.push({
			"id": q.id + " to " + ch.answer,
			"source": q.id.toString(),
			"target": ch.answer.toString()
		});
	}
}
for(var c of dialogue.clips) {
	graph.children.push({
		"id": c.id.toString(),
		"width": width,
		"height": height
	})

	if(c.next) {
		var n = Number.parseInt(c.next.split(':')[1], 10);
		if(n) {
			graph.edges.push({
				"id": c.id + " to " + c.next,
				"source": c.id.toString(),
				"target": c.next.split(':')[1]
			});
		}
	}
}

//child: {"id": "n1", "width": 40, "height": 40}
//edge: {"id": "e1", "source": "n1", "target": "n2"}


klay.layout({
  graph: graph,
  error: console.log,
  success: function(g) {
  	fs.writeFileSync(process.argv[2] + '.layout.json', JSON.stringify(g, null, 2));
  }
});
