# AzureBlobLogger

![Get AzureBlobLogger from nuget](https://img.shields.io/nuget/v/AzureBlobLogger?style=flat-square)

AzureBlobLogger is a simple and light utility for logging then upload to Azure Storage.

## Can I use

AzureBlobLogger is developped by .Net Standard 2.0. You can use it whether your project is developped by .Net Core, .Net Framework, Xamarin or Mono, etc.

## Add it into your project

### Get it via nuget (Recommand)

If you use Visual Studio to manage your projects. You could get this tool from nuget package manager. Just search `AzureBlobLogger` and install it.

Or, you could use nuget command line by the command below:
```
PM> Install-Package AzureBlobLogger
```

### Download and install it
Check the [release](https://github.com/toshiya14/AzureBlobLogger/releases) page to download a latest `nupkg` or `zip` file.
If you use `nupkg` file, you could install it via nuget command line.
If you use `zip` file, there is a `dll` file in the `zip` file. Add it as a reference to your project. (If you take this way, you might meet some dependency problem)

## How to use

After you add AzureBlobLogger library to your project, and resolve all the problems about the dependency.

1. Add `using AzureBlobLogger` to the top of the code that you need to use the library.

2. Before use it, inilialize as like this: `var log = new BlobLogger(constr, container, blobname);`
	1. `constr` is your connection string to the Azure Storage.
	2. `container` is the container name where you want to upload your logs.
	3. `blobname` is the blob name or the path you want to upload your logs.
	
3. Append your log:
	1. Append a line of text:
	
	```C#
	log.Append("This is a log", LogLevel.Information);
	``` 
	
	2. Append a object:
	
	```C#
	// If you pass an object to the first parameter while calling
	// Append, it would automatically convert the object to string
	// with object.ToString() function.
	log.Append(new DateTime(2020, 05, 18), LogLevel.Debug);	
	```
	
	3. LogLevel has several levels, it could be `Text`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.
	
4. You need to flush it manually (Upload to the remote server), by using `log.Flush()`. Be carefully, if the logger try to upload 3 times and still failed. An Exception would be throw.

