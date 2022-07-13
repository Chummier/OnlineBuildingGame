var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub", {
    accessTokenFactory: () => this.loginToken
}).build();

var previewCanvas = document.getElementById("gamePreview");

var worldButtons = document.getElementsByClassName("worldButton");
var selectedLevel = document.getElementById("selectedLevel");
var searchBox = document.getElementById("searchBox");

function HighlightItemInList(index, list) {
    for (let i = 0; i < list.length; i++) {
        list[i].style.border = "none";
    }
    list[index].style.border = "thick solid #0000FF";
}

HighlightItemInList(0, worldButtons);

for (let i = 0; i < worldButtons.length; i++) {
    worldButtons[i].addEventListener("click", function (event) {
        selectedLevel.value = this.value;
        HighlightItemInList(i, worldButtons);
    });
}

function DrawWorld(world, numLayers, worldRows, worldCols, centerX, centerY){
    var ctx = previewCanvas.getContext("2d");
    var img;

    ctx.clearRect(0, 0, previewCanvas.width, previewCanvas.height);

    var m = Math.ceil(centerY) - radiusY;
    var n = Math.ceil(centerX) - radiusX; // top left [m][n] of the game world- [3][3] for example
    for (let y = 0; y < gameCanvas.height; y += imgSize) {
        for (let x = 0; x < gameCanvas.width; x += imgSize) {
            if (m < 0 || n < 0) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);
            } else if (m >= worldRows || n >= worldCols) {
                img = document.getElementById("Air.png");
                ctx.drawImage(img, x, y, imgSize, imgSize);

            } else {
                for (let l = 0; l < numLayers; l++) {
                    img = document.getElementById(world[l][m][n]);
                    ctx.drawImage(img, x, y, imgSize, imgSize);
                    //ctx.strokeText("[" + m + "][" + n + "]", x, y);
                }
            }

            n++;
        }
        n = Math.ceil(centerX) - radiusX;
        m++;
    }

    img = document.getElementById("Player.png");
    ctx.drawImage(img, radiusX * imgSize, radiusY * imgSize, imgSize, imgSize);
    ctx.strokeText(username, radiusX * imgSize + imgSize / 4, radiusY * imgSize + imgSize / 4);
}

searchBox.addEventListener("change", function (event) {
    if (this.value == "") {

    }
});

connection.on("GetWorld", function (world, numLayers, worldRows, worldCols) {
    //DrawWorld(world, numLayers, worldRows, worldCols, 0, 0);
});