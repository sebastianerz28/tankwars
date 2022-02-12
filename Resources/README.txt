TankWars Client
created by Grant Nations, Sebastian Ramirez
contributors: Daniel Kopta

This project was implemented during the Fall 2021 Semester at the University of Utah for CS 3500. Entry dates span
November 16, 2021 to December 5, 2021. 


DESCRIPTION:

	TankWars is a multiplayer game where the goal is to eliminate other players’ tanks. This can be done by hitting a 
	tank with a number of projectiles, or with one laser shot. A laser projectile can be obtained by collecting a powerup
	that spawns on screen. After a player dies, they respawn within a few seconds.

	The TankWars Client manages the interface in the TankWars game. This includes drawing the background, walls, tanks,
	powerups, projectiles, and animations. The client is also responsible for managing computer inputs from the user and 
	sending the appropriate commands back to the server.

	The TankWars Server performs operations such as physics transformations, collision handling, and user connection.
	The server then updates the state of the world for each client that is connected to it at a rate determined by the 
	server settings.




CLIENT


BASIC USAGE:

	Entering a game:

	To enter a game, a user must enter the server address into the “Server Address” text field, enter their player 
	name into the “Player Name” text field, and press the “Connect” button. If there is an error connecting to the
	server, an appropriate message will be displayed and the user will be allowed to try connecting again. If the
	connection is successful, the “Server Address” and “Player Name” fields and the connect button will be disabled.
	Note: a player name may only contain up to 16 characters.
	

	Moving the tank:

	To move the tank while in the game, a user must use the “W”, “A”, “S”, and “D” keys on their keyboard to move up, 
	left, down, and right, respectively. A user may press two keys at the same time, where the most recently pressed 
	key will be the registered movement direction. If a key is lifted while another is still held down, the key being
	held down will become the registered movement direction. This is only applicable to two keys being held down at once;
	any number of keys greater than two being held down at a time will cause the least recently used keys to become 
	unregistered, even when a current key is lifted.
	

	Firing a projectile:

	To fire a projectile, a user must left-click on their mouse at a location on the drawing panel. A user can only fire
	at an interval determined by the server. The damage and 
	

	Powerups:

	Powerups are displayed as chug-jugs and can be found scattered across the screen at locations determined by the server.
	A user must drive their tank over a powerup to collect one. Powerups give a user one laser shot.


	Laser projectiles:

	A laser projectile is obtained by collecting a powerup. A user must right-click at a location on the drawing panel
	using their mouse. A laser will instantly eliminate an enemy tank.


	Exiting the game:

	To exit the game, a user can click on the “X” icon in the right hand corner of the form, or press the “ESC” key on
	their keyboard. Note: no prompt will be displayed, and if a user wishes to join a new game, they must reopen the application.


	Tank health bar:

	The health of a user’s tank is displayed above their tank in the form of a health bar.


	User score:

	A user’s score is the number of enemy tanks that the user has eliminated during the current game. This score is 
	displayed next to the user’s display name, below their tank.




ADDED FEATURES:

	Closing the window using ESC:

	Pressing the “ESC” key on the keyboard while in the game will terminate the application.


	MenuStrip for Controls and Additional Features
	
	Shows a message box showing all controls for the game and briefly lists additional design changes/features.



EXTERNAL REFERENCES:

	DrawingPanel.cs uses Graphics.DrawImage to draw images. Information regarding this method was accessed at:

	https://docs.microsoft.com/en-us/dotnet/api/system.drawing.graphics.drawimage?view=dotnet-plat-ext-6.0




DESIGN DECISIONS:

	Tank death animation:

	The tank death animation draws a series of images that animate a flame at the location of a tank's death. The images
	are kept in an Image array in the DrawingPanel, and when a tank's hp is at 0, the images in this array are drawn
	consecutively on each frame. At the time of a tank’s death, the index of which image to draw is set to 0. The animation
	lasts until the tank respawns (and its hp is no longer 0). These images were accessed for free at:
	https://www.artstation.com/artwork/x2bo1


	Laser animation:

	The laser is animated over the course of 60 frames. Every ten frames it changes colors from red, yellow, green, blue, purple
	which are the colors of the rainbow. Upon each firing of the laser 3 minitaure beams on each side start at a significantly reduced
	length and reduce in length over the course of the animation. This is achieved by setting the length to a fixed size of 60
	and subtracting the frame counter which goes up to a max of 60 which results in a steady reduction in length.


	Health bar:

	The health bar is drawn using two separate rectangles. The first is the health rectangle, which changes color based
	on the health of the tank. The second is grey, and grows in size as the first rectangle shrinks (when a tank loses health).


	Powerups:

	The chug jug icon used is from the video game Fortnite and is licensed free to use for personal use.
	https://flyclipart.com/image-chug-jug-png-631398


	





SERVER



OPERATIONS:

	Updating object locations:

	Tanks and projectiles both need to have their positions updated on each frame. This is accomplished by 
	representing the velocity of the object as a vector and incrementing the object’s position vector with this vector
	on each frame. In the case of a Tank, its position is only adjusted if a control command reveals that the tank is
	moving. A projectile’s position will always be updated, until it collides with a wall or a tank. If a tank were to 
	collide with a wall after updating its position, then its position is not updated.

	Collision detection:

	Collisions are detected as three separate cases. The first case is collisions of a beam and a tank, which uses a 
	method written by Prof. Daniel Kopta, that assumes the area of a tank as a circle of radius TankSize/2 and checks
	for intersection of a beam with that circle. The second case is an object colliding with a wall, such as a tank or a projectile.
	This method takes in the size of the object that is potentially colliding with a wall which allows it to be used for projectiles,
	where the size of a projectile is 0, and tanks, where the size of a tank is TankSize. The third case is a projectile colliding
	with a tank. This method simply checks if the point of the projectile is within the left, right, top and bottom bounds of a tank.
	

	Settings file:

	The settings file is an XML file that contains values to be assigned to the UniverseSize, MSPerFrame, FramesPerShot,
	and RespawnRate instance variables of a server object. It also contains all of the walls to be sent to the client as part
	of the handshake.



EXTERNAL REFERENCES:

	The server uses an XmlReader object to read the settings file. Reference information regarding this object was
	accessed at https://docs.microsoft.com/en-us/dotnet/api/system.xml.xmlreader.read?view=net-6.0.



IMPLEMENTATION DETAILS:

	Random spawn location:

	Tanks and powerups both spawn at random locations within the bounds of the universe’s walls. This is accomplished through
	a private method that creates a vector using a random x and y within bounds, then checks that this point has no collisions
	with a wall. If a collision is detected, then a new point is generated and checked. This process is repeated until no 
	collision is detected, and the location vector is returned.















