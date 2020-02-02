var socket;
var canvas, ctx;

function drawDot(x, y, isAlive) {
    
    if(isAlive) {
        ctx.fillStyle = "rgba(255,255,255,1)";
    } else {
        ctx.fillStyle = "rgba(0,0,0,1)";
    }    
    
    ctx.beginPath();
    ctx.rect(x, y, 20, 20);
    ctx.fill();    
    ctx.closePath();
}

function openWebSocket() {
    var loc = window.location, new_uri;
    if (loc.protocol === "https:") {
        new_uri = "wss:";
    } else {
        new_uri = "ws:";
    }
    new_uri += "//" + loc.host + '/ws';

    socket = new WebSocket(new_uri);
  
    socket.onopen = function () {
        console.log('INFO: WebSocket opened successfully - socket uri ' + new_uri);
    };

    socket.onclose = function () {
        console.log('INFO: WebSocket closed - socket uri ' + new_uri);
        openWebSocket();
    };

    socket.onmessage = function (event) {
        var json = JSON.parse(event.data);
        drawDot(json.X, json.Y, json.IsAlive);
    };
}

var init = function() {
    var container = $("#container");
    container.empty();

    canvas = document.createElement("canvas");
    canvas.setAttribute("id", "tile-canvas");
    canvas.width = 1000;
    canvas.height = 1000;
    container.append(canvas);    
    canvas.style.float = "left";
    canvas.style.position = "relative";
    
    if (canvas.getContext) {
        ctx = canvas.getContext('2d');
    } else {
        document.write("Browser not supported!!");
    }

    openWebSocket();
};

$(function() {
    init();
    
    $("#btnStart").click(function(event){
        event.preventDefault();
        $.get( "/start");
    });
});
