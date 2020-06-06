#ifndef ERRORS_H
#define ERRORS_H

#if defined (__cplusplus)
extern "C" {
#endif

//list of error code
//error list
#define INVALID_SOCKET -2
#define INVALID_PKT -3
#define SEND_ERROR -4
#define RECV_ERROR -5
#define SOCKET_CLOSED -6
#define INVALID_PKT_LIST -7
#define NVS_ERROR -8
#define SETTIME_ERROR -9

#if defined (__cplusplus)
}
#endif

#endif