## PetImagesAspNet

To build the sample run:
```
dotnet build
```

To rewrite the sample for Coyote testing do:
```
coyote rewrite rewrite.coyote.json
```

To run the tests with Coyote do:
```
dotnet test bin/coyote/PetImagesTests.dll
```

More details on this sample coming soon. In the meantime, you can check out another similar
sample [here](https://microsoft.github.io/coyote/tutorials/testing-aspnet-service) to learn how
Coyote can be used to test web apps written in ASP.NET.
