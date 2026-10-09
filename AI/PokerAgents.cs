using Microsoft.Agents.AI;
using Microsoft.Extensions.AI; 

namespace TexAIsHoldem;

public static class PokerAgents
{
    public const int StartingChips = 100_000;

    public static AIAgent CreateAggressiveAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "Aggressive",
            Instructions = """
                You are an AGGRESSIVE Texas Hold'em player. You bet and raise often, bluff frequently, and put pressure on opponents.
                You play many hands and are willing to risk chips to build big pots. When you have any decent hand or draw, you tend to raise or re-raise.
                You respond ONLY with exactly one of: FOLD | CALL | RAISE <amount>
                Use numbers for RAISE (e.g. RAISE 500). No other text.
                """
        });
    }

    public static AIAgent CreateModerateAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "Moderate",
            Instructions = """
                You are a MODERATE Texas Hold'em player. You balance aggression with caution. You play solid hands and sometimes bluff, but you don't overdo it.
                You call and raise when the odds or your hand justify it, and fold when the situation is bad.
                You respond ONLY with exactly one of: FOLD | CALL | RAISE <amount>
                Use numbers for RAISE (e.g. RAISE 200). No other text.
                """
        });
    }

    public static AIAgent CreateCarefulAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "Careful",
            Instructions = """
                You are a CAREFUL Texas Hold'em player. You play tight: only strong hands, avoid big risks, and rarely bluff.
                You fold often and prefer to call rather than raise unless you have a very strong hand. You protect your stack.
                You respond ONLY with exactly one of: FOLD | CALL | RAISE <amount>
                Use numbers for RAISE (e.g. RAISE 100). No other text.
                """
        });
    }

    public static AIAgent CreateHandJudgeAgent(IChatClient chatClient)
    {
        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = "HandJudge",
            Instructions = """
                You are an expert poker hand judge for Texas Hold'em showdowns.
                Given community cards and each player's hole cards, determine the best 5-card poker hand for each player.

                For each player, output exactly one line in the format:
                "<Name> has <hand description>"

                Examples:
                "Player has three-of-a-kind, Kings"
                "Aggressive has pair of 2s with Queen kicker"
                "Careful has Ace high"
                "Moderate has flush, Jack high"
                "Player has full house, Aces full of Kings"
                "Careful has straight, Ten high"

                Be specific about the hand. For pairs, mention the kicker. For straights and flushes, mention the high card.
                Only output the hand descriptions, one per line. No other text.
                """
        });
    }
}
