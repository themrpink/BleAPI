# BleAPI

per .NET Framework 4.8

Se necessario, aggiungere i seguenti riferimenti in CosmedBleLib
![alt text](https://github.com/themrpink/BleAPI/blob/master/img/gestione_riferimenti.png?raw=true)



diagrammi di stato, il primo generico indica il passaggio da active a passive scan e viceversa, il secondo un po´ piú  dettagliato

![alt text](https://github.com/themrpink/BleAPI/blob/master/img/State_Machine_Diagram2.png?raw=true)



Diagramma di stato che tiene conto di tutte le operazioni dello scansione, con bluetooth acceso o spento.
I  questo schea non fa distinzione tra active e passive scan, ma il comportamento è identico.
![alt text](https://github.com/themrpink/BleAPI/blob/master/img/State%20Machine%20Diagram2.png?raw=true)




Le funzionalità del Watcher accessibili dall'Utente e la loro gestione asincrona. Le risorse accessibili e relativa protezione.
![alt text](https://github.com/themrpink/BleAPI/blob/master/img/schema_scanning(1)(4).jpg?raw=true)

Il lock del watcher ha senso solo se i suoi metodi di scan vengono chiamati da un altro thread: per esempio gli eventi, che vengono passati al pool di thread,
hanno come argomento il watcher stesso, per poter gestire per esempio una scansione "aborted". 
In questo caso senza gestione protetta degli accessi alle risorse del watcher ci potrebbero essere conflitti che portano allo stallo dell'applicazione.