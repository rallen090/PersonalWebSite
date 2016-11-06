var path = "";
var promptText = "SERVER: ";

// for allowing shift capitalization
var shifted = false;

// for blocking action until requests end
var waitingForResponse = false;

// for handling command history
var pastCommandIterator = 0;
var pastCommands = [];
var globalCurrentInput = "";

// for handling prompt responses
var waitingForPromptInput = false;
var promptResponseUrl = "";

var appendOutput = function (line, color) {
	var colorStyling = color ? " style='color:" + color + "'" : "";
	$(".output-section").append("<div><span" + colorStyling + ">" + line + "</span></div>");
};

var processCommandResult = function(commandText, data, ignoreHeader) {
	$(".output-section").append(getNewOutputLine(commandText, ignoreHeader));

	// modify path (if applicable)
	if (data.RedirectUrl) {
		window.location.replace(data.RedirectUrl);
	}

	// modify path (if applicable)
	if (data.NewPath !== null) {
		path = data.NewPath;
	}

	// output response lines
	if (data.ResponseLines) {
		$.each(data.ResponseLines, function (index, value) {
			appendOutput(value, data.ColorCode);
		});
	}

	// add new input section after (1) the output is written and (2) the new path is set.
	// we fork on whether it is a prompt or not since the lines differ slightly
	if (data.PromptResponsePostUrl) {
		$(".terminal").append(getNewPromptInputLine());

		// set prompt flag if needed
		waitingForPromptInput = true;
		promptResponseUrl = data.PromptResponsePostUrl;
	} else {
		$(".terminal").append(getNewInputLine());
		waitingForPromptInput = false;
		promptResponseUrl = "";
	}

	// remove the old input
	$(".input-section:not(:last)").remove();

	// reset request flag
	waitingForResponse = false;

	// force scroll bar to bottom
	$(".terminal").scrollTop(10000);
};

var runCommand = function (commandText) {
	// handle clear
	if (commandText === "clear") {
		$(".output-section").empty();
		$(".input-section .input").text("");
		return;
	}

	// track commands for re-use with arrow keys
	if (commandText) {
		pastCommands.unshift(commandText);
	} else {
		$(".output-section").append(getNewOutputLine(commandText));
		$(".terminal").append(getNewInputLine());
		$(".input-section:not(:last)").remove();
		waitingForResponse = false;

		// force scroll bar to bottom
		$(".terminal").scrollTop(10000);

		return;
	}

	waitingForResponse = true;
	pastCommandIterator = 0; // and reset iterator on command
	globalCurrentInput = "";

	var json = JSON.stringify({ "Command": commandText, "Path": path });
	$.post(
		"terminal/command",
		{inputJson: json},
		function (data) {
			processCommandResult(commandText, data, false);
		})
	.fail(function (d) {
		$(".output-section").append(getNewOutputLine(commandText));
		appendOutput("Error connecting to server...", "red");
		$(".terminal").append(getNewInputLine());
		$(".input-section:not(:last)").remove();

		waitingForResponse = false;

		// force scroll bar to bottom
		$(".terminal").scrollTop(10000);
	});
};

var tabComplete = function (partialWord, updatedPath) {
	waitingForResponse = true;

	var json = JSON.stringify({ "PartialWord": partialWord, "Path": updatedPath });
	$.post(
		"terminal/tab/complete",
		{ inputJson: json },
		function (data) {
			if (data) {
				var inputSection = $(".input-section .input");
				inputSection.text(inputSection.text().slice(0, inputSection.text().length - partialWord.length) + data);
			}
			waitingForResponse = false;
		})
	.fail(function (d) {
		waitingForResponse = false;
	});
};

var sendPromptResponse = function (response) {
	waitingForResponse = true;

	var json = JSON.stringify({ "Response": response, "Path": path });
	$.post(
		promptResponseUrl,
		{ inputJson: json },
		function (data) {
			processCommandResult(response, data, true);
			waitingForPromptInput = false;
			promptResponseUrl = "";
		})
	.fail(function (d) {
		$(".output-section").append(getNewOutputLine(response));
		appendOutput("Error connecting to server...", "red");
		$(".terminal").append(getNewInputLine());
		$(".input-section:not(:last)").remove();

		waitingForResponse = false;
		waitingForPromptInput = false;
		promptResponseUrl = "";

		// force scroll bar to bottom
		$(".terminal").scrollTop(10000);
	});
};

var getNewInputLine = function () {
	var displayPath;
	if (path === "") {
		displayPath = "/";
	} else {
		displayPath = path;
	}
	
	var newLine = '\
		<div class="input-section">\
			<span class="input-header">' + promptText + displayPath + '&nbsp;</span>\
			<br/>\
			<span>$&nbsp;</span>\
			<span class="input"></span>\
			<span class="cursor blink" >&nbsp;</span>\
		</div>';

	return newLine;
};

var getNewPromptInputLine = function () {
	var newLine = '\
		<div class="input-section">\
			<span>$&nbsp;</span>\
			<span class="input"></span>\
			<span class="cursor blink" >&nbsp;</span>\
		</div>';

	return newLine;
};

var getNewOutputLine = function (text, ignoreHeader) {
	var displayPath;
	if (path === "") {
		displayPath = "/";
	} else {
		displayPath = path;
	}

	var header = ignoreHeader ? "" : '<span class="input-header">' + promptText + displayPath + '&nbsp;</span>';
	var newLine = '\
		<div>' 
			+ header + '\
			<br/>\
			<span>$</span>\
			<span>' + text + '</span>\
		</div>';

	return newLine;
};

document.onkeypress = function (e) {
	// no inputs allowed during command processing
	if (waitingForResponse) {
		return;
	}

	// toggle if shift is held (down)
	shifted = e.shiftKey;

	e = e || window.event;
	var charCode = (typeof e.which == "number") ? e.which : e.keyCode;
	if (charCode) {
		var currentLine = $(".input-section .input").filter(":last");

		switch (charCode) {
			// space - must be a non-breaking space
			case 32:
				currentLine.html(currentLine.text() + "&nbsp;");
				return;
			// enter - get the result then new line
			case 13:
				var currentInput = currentLine.text();
				if (!waitingForPromptInput) {
					runCommand(currentInput);
				} else {
					sendPromptResponse(currentInput);
				}
				return;
			default:
				break;
		}

		// append the new character (also capitalize based on SHIFT)
		// note: document.onkeydown returns characters in a raw format unlike onkeypress, and so we must 
		// (1) toLowerCase all normal chars and (2) convert special non-alpha-numeric chars to their proper format
		var input = shifted === true ? String.fromCharCode(charCode) : String.fromCharCode(charCode).toLowerCase();
		currentLine.text(currentLine.text() + input);
	}
};

document.onkeyup = function (e) {
	// toggle if shift is held (up)
	shifted = e.shiftKey;
};

// we need onkeydown to get backspace since onkeypress only works with actual characters
document.onkeydown = function (e) {
	// no inputs allowed during command processing
	if (waitingForResponse) {
		return;
	}

	// disable the default space, backspace, tab, and arrow key functions
	if ([8, 9, 32, 37, 38, 39, 40].indexOf(e.keyCode) > -1) {
		e.preventDefault();
	}

	e = e || window.event;
	var charCode = (typeof e.which == "number") ? e.which : e.keyCode;
	// backspace - remove last character
	if (charCode === 8) {
		var currentLine = $(".input-section .input").filter(":last");
		currentLine.text(function (i, v) {
			currentLine.text(v.slice(0, -1));
		});
	}
	// tab - autocomplete
	else if (charCode === 9) {
		var inputSection = $(".input-section .input");
		var text = inputSection.text();
		if (text) {
			var splitInputOnSpace = inputSection.text().split(" ");
			// there should be at least a command + arg to use tab complete
			if (splitInputOnSpace.length > 1) {
				var partialWord = splitInputOnSpace[splitInputOnSpace.length - 1];

				// split on slash next (so we can support A/B/C autocomplete)
				var splitInputOnSlash = partialWord.split("/");
				if (splitInputOnSlash.length > 1) {
					// inspect the path we have to account for navigating backwards (i.e. '..')
					var currentPath = path;
					$.each(splitInputOnSlash, function (index, value) {
						// ignore the last entry, since that is what we will search for
						if (index < splitInputOnSlash.length - 1) {
							if (value === "..") {
								var splits = currentPath.split('/');
								currentPath = splits.splice(0, splits.length - 1).join('/');
							} else {
								currentPath = currentPath + "/" + value;
							}
						}
					});

					// the last string is what we are searching for
					var partialWordWithoutSlashes = splitInputOnSlash[splitInputOnSlash.length - 1];

					//// and then we update the current path with since we are nested further due to the slash(es)
					//var pathExtension = splitInputOnSlash.splice(0, splitInputOnSlash.length - 1);
					//var currentPath = path === "" ? "/" : path;
					tabComplete(partialWordWithoutSlashes, currentPath);
				} else {
					tabComplete(partialWord, path);
				}
			}
		}
	}
	else if (charCode === 32) {
		var line = $(".input-section .input").filter(":last");
		line.html(line.text() + "&nbsp;");
	}
	// down key
	else if (charCode === 40) {
		var inputSection = $(".input-section .input");

		if (pastCommandIterator > 1) {
			pastCommandIterator--;
			inputSection.text(pastCommands[pastCommandIterator - 1]);
		} else {
			pastCommandIterator = Math.max(pastCommandIterator - 1, 0);
			inputSection.text(globalCurrentInput);
		}
	}
	// up key
	else if (charCode === 38) {
		var inputSection = $(".input-section .input");
		if (pastCommandIterator < pastCommands.length) {
			// remember the current line we had if this is the first cycle through our history
			if (pastCommandIterator === 0) {
				globalCurrentInput = inputSection.text();
			}
			
			// then select from history
			inputSection.text(pastCommands[pastCommandIterator]);
			pastCommandIterator++;
		}
	}
};

// redirect on mobile since typing in the terminal is not supported in that medium
window.mobilecheck = function () {
	var check = false;
	(function (a) { if (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino/i.test(a) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(a.substr(0, 4))) check = true; })(navigator.userAgent || navigator.vendor || window.opera);
	
	if (check === true) {
		waitingForResponse = true; // block inputs
		$(".input-section").remove(); // remove the input and sub-header section to fit the content below on mobile
		$(".sub-header").remove(); // redirect
		appendOutput("Mobile browser detected!", "lightcoral");
		appendOutput("Terminal is not supported on mobile.");
		appendOutput("Redirecting to web view in 7...");

		var countDown = 6;
		var redirectCountdown = function() {
			if (countDown > 0) {
				appendOutput(countDown + "...");
				countDown--;
				setTimeout(redirectCountdown, 1000);
			} else {
				appendOutput("Redirecting!");
				window.location = "home";
			}
		};
		setTimeout(redirectCountdown, 1000);
	}
};

$(document).ready(function () {
	window.mobilecheck();
});