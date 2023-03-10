#include <Windows.h>
#include <stdio.h>
#include <conio.h>
#include <tchar.h>

#define BUFSIZE 512

int _tmain(int argc, char* argv[])
{
	HANDLE hPipe;
	LPTSTR lpvMessage = (LPTSTR)TEXT("Default message from client");
	TCHAR chBuf[BUFSIZE];
	BOOL fSuccess = FALSE;
	DWORD cbRead, cbToWrite, cbWritten, dwMode;
	LPTSTR lpszPipename = (LPTSTR)TEXT("\\\\.\\pipe\\mynamedpipe");

	if (argc > 1)
	{
		lpvMessage = (LPTSTR)argv[1];
	}

	// Try to open a named pipe; wait for it, if necessary.
	while (1)
	{
		hPipe = CreateFile(
			lpszPipename,   // pipe name 
			GENERIC_READ |  // read and write access 
			GENERIC_WRITE,
			0,              // no sharing 
			NULL,           // default security attributes
			OPEN_EXISTING,  // opens existing pipe 
			0,              // default attributes 
			NULL);          // no template file

		// Break if the pipe handle is valid. 

		if (hPipe != INVALID_HANDLE_VALUE)
			break;

		if (GetLastError() != ERROR_PIPE_BUSY)
		{
			printf("Could not open pipe. GLE=%d\n", GetLastError());
			return -1;
		}

		// All pipe instances are busy, so wait for 20 seconds. 

		if (!WaitNamedPipe(lpszPipename, 20000))
		{
			printf("Could not open pipe: 20 second wait timed out.");
			return -1;
		}
	}

	// The pipe connected; change to message-read mode. 
	dwMode = PIPE_READMODE_MESSAGE;
	fSuccess = SetNamedPipeHandleState(
		hPipe,    // pipe handle 
		&dwMode,  // new pipe mode 
		NULL,     // don't set maximum bytes 
		NULL);    // don't set maximum time 

	if (!fSuccess)
	{
		printf("SetNamedPipeHandleState failed. GLE=%d\n", GetLastError());
		return -1;
	}

	// Send a message to the pipe server

	cbToWrite = (lstrlen(lpvMessage)+1) * sizeof(TCHAR);
	_tprintf(TEXT("Sending %d byte message: \"%s\"\n"), cbToWrite, lpvMessage);

	fSuccess = WriteFile(
		hPipe,                  // pipe handle 
		lpvMessage,             // message 
		cbToWrite,              // message length 
		&cbWritten,             // bytes written 
		NULL);                  // not overlapped 

	if (!fSuccess)
	{
		printf("WriteFile to pipe failed. GLE=%d\n", GetLastError());
		return -1;
	}

	printf("\nMessage sent to server, receiving reply as follows:\n");

	do {
		fSuccess = ReadFile(
			hPipe,
			chBuf,
			BUFSIZE * sizeof(TCHAR),
			&cbRead,
			NULL);

		if (!fSuccess && GetLastError() != ERROR_MORE_DATA)
			break;

		_tprintf(TEXT("\"%s\"\n"), chBuf);

	} while (!fSuccess); // repeat loop if ERROR_MORE_DATA 

	if (!fSuccess)
	{
		_tprintf(TEXT("ReadFile from pipe failed. GLE=%d\n"), GetLastError());
		return -1;
	}

	printf("\n<End of message, press ENTER to terminate connection and exit>");
	_getch();

	CloseHandle(hPipe);

	return 0;
}
