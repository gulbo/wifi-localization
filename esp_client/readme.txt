***************** PROTOCOL INITIALIZATION *************
Client: HI id(4B) MAC_ADDR(6B)
Server: OK n_mac(4B) list_of_mac(each is 6B) -> il server rimane in attesa di tutte le schedine e appena tutte si sono connesse e hanno inviato HELLO risponde con questo messaggio
Client: .... sniffing phase + ping (x1) to server, count number of mac identified. Insert a pause of 500ms between start sniffing and send ping .... -> espBoards invia un messaggio con scritto PING al server e sniffa tutti i pacchetti. Ad ogni MAC riscontrato non flaggato, incremento un contatore. Appena il contatore sale a n o scade un timer, vado avanti
Client: DE n_esp32_found (4B) list_of_not_found_mac(6B each) -> DE = detected. Invio schede rilevate
// right now we are NOT sending the list of not found. it was commented in the code
// ALWAYS <DE 0>
Server: alternatives
		1. GO (lato server faccio la sync tra tutti i thread) timestamp(4B) -> procedura e sincronizzazione completata con successo
		2. RT -> retry... Riparte dalla riga 3, dallo sniffing.
	
Tutti i testi inviati NON contengono il terminatore di stringa \0


*************** SENDER TASK LOOP *******************
Client: id(4B) #packets(4B)
						MAC_ADDRESS_BYTES + RSSI_BYTES + SSID_LENGTH_BYTES + SSID_BYTES + TIMESTAMP_BYTES + CHECKSUM_BYTES];