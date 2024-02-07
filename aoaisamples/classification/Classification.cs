
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.SemanticKernel;
using System;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;

internal class Classification
{
    public static void RunClassification(string[] args)
    {
        // Read an Excel file using the OpenXML SDK
        string filePath = "C:\\Users\\aveekr\\aoai_samples\\aoai_samples\\aoaisamples\\classification\\Get_Merchants_clean.xlsx";

        using (SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, false))
        {
            WorkbookPart workbookPart = document.WorkbookPart;
            WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();

            // Define the range of cells to read
            int startRow = 2; // Assuming the merchant names start from the first row
            int endRow = 10; // Assuming the end row is 10
            int merchantNameColumn = 1; // Assuming the merchant names are in the first column

            // Read the merchant names
            List<string> merchantNames = new List<string>();
            foreach (Row row in sheetData.Elements<Row>())
            {
                if (row.RowIndex >= startRow && row.RowIndex <= endRow)
                {
                    Cell cell = row.Elements<Cell>().FirstOrDefault(c => GetColumnIndex(c.CellReference) == merchantNameColumn);
                    if (cell != null)
                    {
                        string merchantName = GetCellValue(cell, workbookPart);
                        if (!string.IsNullOrEmpty(merchantName))
                        {
                            merchantNames.Add(merchantName);
                        }
                    }
                }
            }
          RunAsync(merchantNames).Wait();
           
        }
    }

    public static async Task RunAsync(List<String> merchantNames)
    {
      
    // Use the merchant names for further processing
            // You can now pass the merchant names to your semantic kernel or GPT-4 model for categorization
            // create a semantic kernel plugin using GPT-4 model for categorization
            Kernel kernel = InitializeKernel();
            string promptTemplate = @"##Instructions
                             Based on merchent name categorize them 
                             into different categories. 
                             IN YOUR RESPONSE PLEASE INCLUDE THE CATEGORY AND SUB-CATEGORY of the merchant name input only
                            
                             
                             Here are some examples:
                             
                             MERCHENT NAME :'UNITED' 
                             
                            RESPONSE:
                             MERCHENT NAME :'UNITED'
                             CATEGORY: Transportation 
                             SUB-CATEGORY : 'AIR' 
                             REASON : 'UNITED is an airline and the transaction is for air travel'
                            
                              MERCHENT NAME :'WALMART'
                             RESPONSE :
                                MERCHENT NAME :'WALMART'
                                CATEGORY: Retail
                                SUB-CATEGORY : 'Grocery'
                                REASON : 'WALMART is a retail store and the transaction is for grocery'
                            
                              MERCHENT NAME :'BKG*HOTEL AT BOOKING.C'
                             RESPONSE :
                                MERCHENT NAME :'BKG*HOTEL AT BOOKING.C'
                                CATEGORY: Agency Fee
                                SUB-CATEGORY : 'None'
                                REASON : 'BKG*HOTEL AT BOOKING.C is a booking agency and the transaction is for agency fee'
                            
                            MERCHENT NAME :'HILTON GARDEN INN'
                            RESPONSE :
                                MERCHENT NAME :'HILTON GARDEN INN'
                                CATEGORY: 'Lodging'
                                SUB-CATEGORY : 'Room and Tax'
                                REASON : 'HILTON GARDEN INN is a hotel and the transaction is for room and tax'
                            
                             
                            
                            MERCHENT NAME :{{$merchent_name}}   
                            RESPONSE :
                            
                                     
                             ";
            var func = kernel.CreateFunctionFromPrompt(promptTemplate,new OpenAIPromptExecutionSettings() { MaxTokens = 100, Temperature = 0.4, TopP = 1 });
            for (int i = 0; i < merchantNames.Count; i++)
            {   
                Console.WriteLine($"Processing merchant name: {merchantNames[i]}");
                
                var result = await func.InvokeAsync(kernel, new() { ["merchent_name"] = merchantNames[i] });
                Console.WriteLine(result.GetValue<string>());
                Console.WriteLine("-------------------------------------------------");
            }
            // Perform further processing using the merchant names and the kernel
    }
            // ...
    private static int GetColumnIndex(string cellReference)
    {
        string columnName = Regex.Replace(cellReference, @"[\d]", string.Empty);
        int columnIndex = -1;
        int factor = 1;
        for (int i = columnName.Length - 1; i >= 0; i--)
        {
            columnIndex += factor * (columnName[i] - 'A' + 1);
            factor *= 26;
        }
        return columnIndex;
    }

    private static string GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            SharedStringTablePart sharedStringPart = workbookPart.SharedStringTablePart;
            if (sharedStringPart != null)
            {
                SharedStringItem sharedStringItem = sharedStringPart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(cell.CellValue.InnerText));
                if (sharedStringItem != null)
                {
                    return sharedStringItem.Text.Text;
                }
            }
        }
        return cell.CellValue?.InnerText;
    }

    private static Kernel InitializeKernel()
    {
    var configuration = new ConfigurationBuilder()
    .AddUserSecrets("38200dae-db69-441e-b03a-86f740caac94")
    .Build();

        string apiKey = configuration["AzureOpenAI:ApiKey"];
        string deploymentName = configuration["AzureOpenAI:DeploymentName"];
        string endpoint = configuration["AzureOpenAI:Endpoint"];

        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey,
                modelId: "gpt-4")
            .Build();

        return kernel;
    }
}
        
       


    


