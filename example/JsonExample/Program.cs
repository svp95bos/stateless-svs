namespace JsonExample
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("Creating member from JSON");
            Member aMember = Member.FromJson("{ \"State\":\"1\",\"Name\":\"Jay\"}");

            Console.WriteLine($"Member {aMember.Name} created, membership state is {aMember.State}");

            aMember.Suspend();
            aMember.Reactivate();
            aMember.Terminate();

            Console.WriteLine("Member JSON:");

            string jsonString = aMember.ToJson();
            Console.WriteLine(jsonString);

            Member anotherMember = Member.FromJson(jsonString);

            if (aMember.Equals(anotherMember))
            {
                Console.WriteLine("Members are equal");
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}

