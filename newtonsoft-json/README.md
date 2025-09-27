# Ivy.Newtonsoft.Json 

Web application created using [Ivy](https://github.com/Ivy-Interactive/Ivy). 

Ivy is a web framework for building interactive web applications using C# and .NET.

## Run
```
dotnet watch
```
## Usage
A sample json file is loaded in the editor. The values can be changed. When the button is clicked, the json is deserialized and the values appear on the second card.
We can also change the values on the UI card and serialize it, in which case, the json value is written to the editor.

In both cases, if an error occurs, a toast shows the error.

## Deploy

```
ivy deploy
```