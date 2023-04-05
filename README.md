# AI-TextDetector

Install .NET Core SDK on your system. You can download it from https://dotnet.microsoft.com/download/dotnet-core/3.1

Create a text file with the passages you want to classify enclosed in quotations, like this:
"passage here"

Run the tool from the terminal/command line using the following command:
dotnet run

Enter the name of the text file when prompted. The file should be in the same directory where you are running the tool from.

Enter your OpenAI API authorization bearer token when prompted. You can get the token from here https://platform.openai.com/ai-text-classifier

The tool will send the passages to the OpenAI API for classification and print out the top classification result for each passage along with the confidence percentage.

The classification results will be one of the following:
very unlikely
unlikely
unclear if it is
possibly
likely

The confidence percentage indicates how confident the AI model is in the classification. Higher the percentage, higher the confidence.

You can modify the input parameters like maximum tokens, temperature, etc. in the RequestPayload class to tune the model's output. Refer to the OpenAI API documentation for more details.

Let me know if you have any other questions!
