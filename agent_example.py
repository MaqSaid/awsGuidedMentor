"""
Simple Strands Agent Example
-----------------------------
This agent uses Amazon Bedrock (Claude 4 Sonnet) with community tools.
Make sure you have:
  1. pip install strands-agents strands-agents-tools
  2. export AWS_BEDROCK_API_KEY=your_key  (or AWS credentials configured)
  3. Model access enabled in Bedrock console
"""

from strands import Agent
from strands_tools import calculator, http_request

# Create an agent with tools — defaults to Bedrock Claude 4 Sonnet
agent = Agent(
    tools=[calculator, http_request],
    system_prompt="You are a helpful assistant that can do math and fetch web data.",
)

# Ask the agent a question
response = agent("What is 42 * 17 + 256?")
print(response)

# The agent remembers context across turns
response = agent("Now divide that result by 3")
print(response)
