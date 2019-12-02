namespace Rek.FoodSystem {
    public class Command
    {
        public ulong sender;
        public string content;

        public Command() {}
        
        public Command(ulong sender, string content) {
            this.sender = sender;
            this.content = content;
        }
    }
}
