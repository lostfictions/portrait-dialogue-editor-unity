'use strict';

var klay = require('klayjs');
var fs = require('fs');

var parsed = JSON.parse(fs.readFileSync(process.argv[2]));

var width = 200;
var height = 200;

function prepareGraph(dialogue) {
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
  
  return graph;
}

function applyLayout(layout, dialogue) {
  for(var c of layout.children) {
    var id = Number.parseInt(c.id, 10);
    var i = dialogue.questions.findIndex(e => e.id === id);
    if(i !== -1) {
      dialogue.questions[i].x = c.x;
      dialogue.questions[i].y = c.y;
    }
    else {
      i = dialogue.clips.findIndex(e => e.id === id);
      if(i === -1) {
        throw new Error("Can't find index referred to by layout!");
      }
      dialogue.clips[i].x = c.x;
      dialogue.clips[i].y = c.y;
    }
  }

  fs.writeFileSync(process.argv[2] + '.layout.json', JSON.stringify(dialogue, null, 2));  
}


var graph = prepareGraph(parsed);

var layout;

klay.layout({
  graph: graph,
  error: console.log,
  success: function(g) {
    layout = g;
    // applyLayout(g, parsed);
    // fs.writeFileSync(process.argv[2] + '.layout.json', JSON.stringify(g, null, 2));
  }
});

applyLayout(layout, parsed);