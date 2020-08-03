using System;
using System.Collections;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using CyberSource.Clients;
using CyberSource.Clients.SoapServiceReference;

namespace PlayWithCyberSource
{
    class Program
    {
        static void Main(string[] args)
        {
            var request = new RequestMessage();
            // request.merchantID = "sqid_payments_sandbox";
            request.merchantReferenceCode = Guid.NewGuid().ToString();


            request.ccAuthService = new CCAuthService();
            request.ccAuthService.run = "true";
            request.ccCaptureService = new CCCaptureService();
            request.ccCaptureService.run = "true";

            var billTo = new BillTo();
            billTo.firstName = "Jane";
            billTo.lastName = "Smith";
            billTo.email = "null@cybersource.com";
            billTo.country = "US";
            billTo.city = "Mountain View";
            billTo.street1 = "1295 Charleston Road";
            billTo.state = "CA";
            billTo.postalCode = "94043";
            request.billTo = billTo;

            var card = new Card();
            card.accountNumber = "4111111111111111";
            card.expirationMonth = "12";
            card.expirationYear = "2020";
            request.card = card;

            var purchaseTotals = new PurchaseTotals();
            purchaseTotals.currency = "USD";
            request.purchaseTotals = purchaseTotals;

            request.item = new Item[1];
            var item = new Item
            {
                id = "0",
                unitPrice = "29.95"
            };

            request.item[0] = item;
            
            try
            {
                ReplyMessage reply = SoapClient.RunTransaction(request);
                SaveOrderState();
                ProcessReply(reply);
            }
            catch (CryptographicException ce)
            {
                SaveOrderState();
                Console.WriteLine(ce.ToString());
            }
            catch (WebException we)
            {
                SaveOrderState();

                Console.WriteLine(we.ToString());
            }

            Console.WriteLine("Press Return to end...");
            Console.ReadLine();
        }

        private  static  void SaveOrderState() { }

        private static void ProcessReply(ReplyMessage reply)
        {
            var decision = reply.decision.ToUpper();
            var template = GetTemplate(decision);

            var content = GetContent(reply);

            Console.WriteLine(template, content);
        }

        private static string GetContent(ReplyMessage reply)
        {
            int reasonCode = int.Parse(reply.reasonCode);
            switch (reasonCode)
            {
                // Success
                case 100:
                    return (
                        "\nRequest ID: " + reply.requestID);

                // Missing field(s)
                case 101:
                    return (
                        "\nThe following required field(s) are missing: " +
                        EnumerateValues(reply.missingField));

                // Invalid field(s)
                case 102:
                    return (
                        "\nThe following field(s) are invalid: " +
                        EnumerateValues(reply.invalidField));

                // Insufficient funds
                case 204:
                    return (
                        "\nInsufficient funds in the account.  Please use a " +
                        "different card or select another form of payment.");

                // add additional reason codes here that you need to handle
                // specifically.

                default:
                    // For all other reason codes, return an empty string,
                    // in which case, the template will be displayed with no
                    // specific content.
                    return (string.Empty);
            }
        }

        private static string EnumerateValues(string[] array)
        {
            var sb = new StringBuilder();
            foreach (string val in array)
            {
                sb.Append(val + "\n");
            }

            return sb.ToString();
        }

        private static string GetTemplate(string decision)
        {
            if ("ACCEPT" == decision)
            {
                return "The order succeeded.{0}";
            }

            if ("REJECT" == decision)
            {
                return"Your order was not approved.{0}";
            }

            return "Your order could not be completed at this time.{0} \nPlease try again later.";
        }
    }
}
