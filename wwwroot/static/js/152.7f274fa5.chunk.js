"use strict";
(self["webpackChunkhavencnc"] = self["webpackChunkhavencnc"] || []).push([[152],{

/***/ 1152:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SignalRPositionUpdater: () => (/* binding */ SignalRPositionUpdater)
/* harmony export */ });
/* harmony import */ var _data_CNCPoint__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(9271);
/* harmony import */ var _data_Common_CoordinateType__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(623);
/* harmony import */ var _data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(6465);
/* harmony import */ var _data_SignalRStateData__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(7768);
/* harmony import */ var _machine_Machine__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(4225);
/* harmony import */ var _CoordinateUtil__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(7961);
/* harmony import */ var _Logger__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(4002);
/* harmony import */ var _SignalRManager__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(7761);
/* harmony import */ var _statemanager_StateManager__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(2331);
var _SignalRPositionUpdater;/**
 * SignalRPositionUpdater - Listens for DRO and Heartbeat events and updates machine position
 * This is used in Centroid mode to keep the UI in sync with actual machine position
 */class SignalRPositionUpdater{// Track if we've received first position from heartbeat
constructor(){this._isListening=false;this._lastConnectionState=_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Disconnected;this._tolerance=0.0001;// Tolerance for coordinate comparison (machine units)
this._hasReceivedFirstPosition=false;/**
     * Handle DRO event from SignalR
     */this._handleDROEvent=event=>{try{const data=event.data;// Create CNCPoint from DRO data
// DRO data comes in MachineHome coordinates from Centroid
// (relative to machine home corner, not machine zero)
const machineHomePosition=new _data_CNCPoint__WEBPACK_IMPORTED_MODULE_0__/* .CNCPoint */ .V(data.axis1,// X
data.axis2,// Y
data.axis3,// Z
_data_Common_CoordinateType__WEBPACK_IMPORTED_MODULE_1__/* .CoordinateType */ .t.MachineHome);// Convert to MachineZero coordinates (the system currentMachinePoint uses)
const newPosition=_CoordinateUtil__WEBPACK_IMPORTED_MODULE_5__.CoordinateUtil.toMachineZero(machineHomePosition);// Update position if it's different
this._updatePositionIfDifferent(newPosition);}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'Error handling DRO event:',error);}};/**
     * Handle Heartbeat event from SignalR
     */this._handleHeartbeatEvent=event=>{try{const data=event.data;_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,`Heartbeat received: isConnected=${data.isConnected}, hasPosition=${!!data.position}, `+`position=${data.position?`X:${data.position.x.toFixed(4)} Y:${data.position.y.toFixed(4)} Z:${data.position.z.toFixed(4)}`:'null'}`);// Update connection state in MachineStateData
_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected=data.isConnected;// Log when not connected
if(!data.isConnected){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'Heartbeat: Server not connected to CNC');return;}// Log when position is missing
if(!data.position){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'Heartbeat: Position data is null or missing');return;}// Create CNCPoint from heartbeat position data
// Heartbeat position comes in MachineHome coordinates
const machineHomePosition=new _data_CNCPoint__WEBPACK_IMPORTED_MODULE_0__/* .CNCPoint */ .V(data.position.x,data.position.y,data.position.z,_data_Common_CoordinateType__WEBPACK_IMPORTED_MODULE_1__/* .CoordinateType */ .t.MachineHome);// Convert to MachineZero coordinates (the system currentMachinePoint uses)
const newPosition=_CoordinateUtil__WEBPACK_IMPORTED_MODULE_5__.CoordinateUtil.toMachineZero(machineHomePosition);_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,`Heartbeat position converted to MachineZero: X:${newPosition.x.toFixed(4)} Y:${newPosition.y.toFixed(4)} Z:${newPosition.z.toFixed(4)}`);// Update position if it's different
this._updatePositionIfDifferent(newPosition);}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'Error handling Heartbeat event:',error);}};/**
     * Verify server is actually responding (not just SignalR connected)
     */this._verifyServerConnection=async()=>{try{const machine=_machine_Machine__WEBPACK_IMPORTED_MODULE_4__.Machine.getInstance();const isConnected=await machine.system.IsConnectedToServer();// Update connection state
if(_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected!==isConnected){_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected=isConnected;if(isConnected){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.Network,'Server connection verified');}}}catch(error){// API call failed - definitely not connected
if(_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected){_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected=false;_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.Network,'Server verification failed:',error);}}};/**
     * Handle reconnection - re-establish fixture/work zero with the machine
     */this._handleReconnection=async()=>{try{const machine=_machine_Machine__WEBPACK_IMPORTED_MODULE_4__.Machine.getInstance();// Re-establish the fixture point (work zero)
await machine.movement.SetCurrentFixturePoint();_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.Network,'Re-established fixture point after reconnection');}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.Network,'Failed to re-establish fixture point after reconnection:',error);}};// Private constructor for singleton
// Watch for connection state changes to handle reconnection
this._watchConnectionState();}static get Instance(){if(!SignalRPositionUpdater._instance){SignalRPositionUpdater._instance=new SignalRPositionUpdater();}return SignalRPositionUpdater._instance;}/**
     * Start listening for DRO and Heartbeat position updates
     */startListening(){if(this._isListening){return;}// Add listeners for DRO and Heartbeat events
_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .SignalRManager */ .g.Instance.addEventListener('DROEvent',this._handleDROEvent);_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .SignalRManager */ .g.Instance.addEventListener('Heartbeat',this._handleHeartbeatEvent);this._isListening=true;_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'SignalRPositionUpdater started listening for DRO and Heartbeat events');}/**
     * Stop listening for DRO and Heartbeat position updates
     */stopListening(){if(!this._isListening){return;}_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .SignalRManager */ .g.Instance.removeEventListener('DROEvent',this._handleDROEvent);_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .SignalRManager */ .g.Instance.removeEventListener('Heartbeat',this._handleHeartbeatEvent);this._isListening=false;_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,'SignalRPositionUpdater stopped listening for DRO and Heartbeat events');}/**
     * Update machine position if it differs from current position
     */_updatePositionIfDifferent(newPosition){const currentPosition=_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.currentMachinePoint;// Compare coordinates with tolerance
const xDifferent=Math.abs(currentPosition.x-newPosition.x)>this._tolerance;const yDifferent=Math.abs(currentPosition.y-newPosition.y)>this._tolerance;const zDifferent=Math.abs(currentPosition.z-newPosition.z)>this._tolerance;// Only update if at least one coordinate has changed significantly
if(xDifferent||yDifferent||zDifferent){_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.currentMachinePoint=newPosition;_statemanager_StateManager__WEBPACK_IMPORTED_MODULE_8__.StateManager.propertyChanged(_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance,'currentMachinePoint');// Log first position update from heartbeat
if(!this._hasReceivedFirstPosition){this._hasReceivedFirstPosition=true;_Logger__WEBPACK_IMPORTED_MODULE_6__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_6__.LogCategory.DataManager,`First position from heartbeat: X=${newPosition.x.toFixed(4)}, Y=${newPosition.y.toFixed(4)}, Z=${newPosition.z.toFixed(4)}`);}}// Position updates happen constantly - no logging needed for subsequent updates
}/**
     * Watch for connection state changes to handle reconnection
     */_watchConnectionState(){// Check connection state periodically
setInterval(async()=>{const currentState=_data_SignalRStateData__WEBPACK_IMPORTED_MODULE_3__.SignalRStateData.Instance.connectionState;// Update MachineStateData connection state based on SignalR state
if(currentState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Disconnected||currentState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Reconnecting){_data_MachineStateData__WEBPACK_IMPORTED_MODULE_2__.MachineStateData.Instance.isConnected=false;}// Detect when we transition from Reconnecting/Disconnected to Connected
if(currentState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Connected&&(this._lastConnectionState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Reconnecting||this._lastConnectionState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Disconnected)){await this._handleReconnection();}// When SignalR is connected, verify server is actually responding
if(currentState===_SignalRManager__WEBPACK_IMPORTED_MODULE_7__/* .ConnectionState */ .K.Connected){await this._verifyServerConnection();}this._lastConnectionState=currentState;},1000);// Check every second
}/**
     * Check if currently listening
     */get isListening(){return this._isListening;}}_SignalRPositionUpdater=SignalRPositionUpdater;SignalRPositionUpdater._instance=void 0;

/***/ })

}]);
//# sourceMappingURL=152.7f274fa5.chunk.js.map