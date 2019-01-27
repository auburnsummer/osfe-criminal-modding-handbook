# OSFE Illegal Modding Tools

_You enter the bar._

The bartender looks at you strangely. He's smoking a cigarette and has unshaven
hair. The entire bar is lit dimly, the electric lights flickering intermittedly.
He asks: "Light, or dark?"


```
"Light."
"Dark."
">The moon is bright tonight."
```

You said the hidden codeword. The bartender nods once, and presses on the wall
behind him. It's a secret panel! You can see a blue glow behind the panel...

```
>Enter the secret base.
Leave the bar.
```

You enter the secret base. Inside is a bunch of computers. You reach to one of
them; on the monitor it says: `WELCOME TO THE ILLEGAL MODDING CLUB`. You've found
the infamous illegal modding club! On the next monitor, there are instructions
for how to set up the illegal modding tools!

# Set up

Clone this repo. You'll need to set up the references:

- Get the references to `Assembly-CSharp.dll`, `UnityEngine.dll` from the actual game.
- Then download a copy of MonoMod (https://github.com/0x0ade/MonoMod) and make references
  for that as well.
  
Then when you build it, you should get a .dll file out which MonoMod can use. 