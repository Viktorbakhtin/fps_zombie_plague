import asyncio
import json
from mcp import ClientSession
from mcp.client.streamable_http import streamable_http_client

async def main():
    async with streamable_http_client('http://127.0.0.1:8080/mcp') as streams:
        read, write = streams[:2]
        async with ClientSession(read, write) as session:
            await session.initialize()
            tools = await session.list_tools()
            names = [tool.name for tool in tools.tools]
            print(json.dumps({'tool_count': len(names), 'tools': names}))
            if 'manage_scene' in names:
                result = await session.call_tool('manage_scene', {'action': 'get_active'})
                print(result.model_dump_json())

asyncio.run(main())
