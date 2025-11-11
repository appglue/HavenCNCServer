"use strict";
(self["webpackChunkhavencnc"] = self["webpackChunkhavencnc"] || []).push([[697],{

/***/ 5697:
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   SignalRIOUpdater: () => (/* binding */ SignalRIOUpdater)
/* harmony export */ });
/* harmony import */ var _data_IOStateData__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(6898);
/* harmony import */ var _Logger__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(4002);
/* harmony import */ var _SignalRManager__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(7761);
var _SignalRIOUpdater;/**
 * SignalRIOUpdater - Listens for I/O status messages and updates IOStateData
 * Handles messages like:
 * - "S198 Output 7 On" 
 * - "S134 Input 7 Closed"
 * - "S135 Input 7 Open"
 */class SignalRIOUpdater{constructor(){this._isListening=false;/**
     * Handle MessageEvent from SignalR and parse I/O status messages
     */this._handleMessageEvent=event=>{try{const message=event.data.message;if(!message){return;}_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Checking message: "${message}"`);// Parse I/O status messages
const ioUpdate=this._parseIOStatusMessage(message);if(ioUpdate){_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Parsed I/O update - type=${ioUpdate.type}, number=${ioUpdate.number}, isActive=${ioUpdate.isActive}`);// Update I/O state immediately
this._updateIOState(ioUpdate);}}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,'Error handling I/O message event:',error);}};}// Private constructor for singleton pattern
/**
     * Get the singleton instance
     */static get Instance(){if(!SignalRIOUpdater._instance){SignalRIOUpdater._instance=new SignalRIOUpdater();}return SignalRIOUpdater._instance;}/**
     * Start listening for I/O status messages
     */startListening(){if(this._isListening){return;}_SignalRManager__WEBPACK_IMPORTED_MODULE_2__/* .SignalRManager */ .g.Instance.addEventListener('MessageEvent',this._handleMessageEvent);this._isListening=true;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,'SignalRIOUpdater started listening for I/O status messages');}/**
     * Stop listening for I/O status messages
     */stopListening(){if(!this._isListening){return;}_SignalRManager__WEBPACK_IMPORTED_MODULE_2__/* .SignalRManager */ .g.Instance.removeEventListener('MessageEvent',this._handleMessageEvent);this._isListening=false;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,'SignalRIOUpdater stopped listening for I/O status messages');}/**
     * Parse I/O status messages into structured data
     * @param message The raw message string (e.g., "S198 Output 7 On")
     * @returns Parsed I/O update or null if not an I/O message
     */_parseIOStatusMessage(message){// Match patterns (with or without 'S' prefix):
// S198 Output 7 On  OR  5198 Output 7 On
// S134 Input 7 Closed  OR  5134 Input 7 Closed
// S135 Input 7 Open  OR  5135 Input 7 Open
const outputPattern=/^S?\d+\s+Output\s+(\d+)\s+(On|Off)$/i;const inputPattern=/^S?\d+\s+Input\s+(\d+)\s+(Closed|Open)$/i;let match=message.match(outputPattern);if(match){const number=parseInt(match[1],10);const isActive=match[2].toLowerCase()==='on';_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Parsed OUTPUT ${number} = ${isActive?'ON':'OFF'} from message: "${message}"`);return{type:'output',number,isActive,originalMessage:message};}match=message.match(inputPattern);if(match){const number=parseInt(match[1],10);const isActive=match[2].toLowerCase()==='closed';_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Parsed INPUT ${number} = ${isActive?'CLOSED':'OPEN'} from message: "${message}"`);return{type:'input',number,isActive,originalMessage:message};}_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Message did NOT match I/O patterns: "${message}"`);return null;}/**
     * Enhance the MessageEvent with custom I/O information
     * Updates the message to include the I/O name and custom active/inactive text
     */_enhanceMessageWithIOInfo(event,update){if(!_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.isInitialized){return;}try{let ioItem;let displayName;let statusText;if(update.type==='input'){var _ioItem,_ioItem2,_ioItem3,_ioItem4;ioItem=_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.getInputByNumber(update.number);// Use custom name if available
displayName=this._formatIOName((_ioItem=ioItem)===null||_ioItem===void 0?void 0:_ioItem.name,(_ioItem2=ioItem)===null||_ioItem2===void 0?void 0:_ioItem2.centroidName);if(!displayName){displayName=`Input ${update.number}`;}else{displayName=`Input ${update.number} (${displayName})`;}// Use custom active/inactive text
statusText=update.isActive?((_ioItem3=ioItem)===null||_ioItem3===void 0?void 0:_ioItem3.activeText)||'Active':((_ioItem4=ioItem)===null||_ioItem4===void 0?void 0:_ioItem4.inactiveText)||'Inactive';}else{var _ioItem5,_ioItem6,_ioItem7,_ioItem8;ioItem=_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.getOutputByNumber(update.number);// Use custom name if available
displayName=this._formatIOName((_ioItem5=ioItem)===null||_ioItem5===void 0?void 0:_ioItem5.name,(_ioItem6=ioItem)===null||_ioItem6===void 0?void 0:_ioItem6.centroidName);if(!displayName){displayName=`Output ${update.number}`;}else{displayName=`Output ${update.number} (${displayName})`;}// Use custom active/inactive text
statusText=update.isActive?((_ioItem7=ioItem)===null||_ioItem7===void 0?void 0:_ioItem7.activeText)||'Active':((_ioItem8=ioItem)===null||_ioItem8===void 0?void 0:_ioItem8.inactiveText)||'Inactive';}// Update the message event data with enhanced information
event.data.message=`${displayName}: ${statusText}`;}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,'Error enhancing message with I/O info:',error);}}/**
     * Update IOStateData with the parsed I/O status
     */_updateIOState(update){if(!_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.isInitialized){_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,'IOStateData not initialized - skipping I/O update');return;}try{if(update.type==='input'){const input=_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.getInputByNumber(update.number);const oldState=input===null||input===void 0?void 0:input.isActive;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Updating input ${update.number} from ${oldState} to ${update.isActive}`);_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.updateInputState(update.number,update.isActive);const inputName=this._formatIOName(input===null||input===void 0?void 0:input.name,input===null||input===void 0?void 0:input.centroidName);const displayName=inputName?`input ${update.number} (${inputName})`:`input ${update.number}`;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`Updated ${displayName} to ${update.isActive?'ACTIVE':'INACTIVE'} via SignalR`);}else if(update.type==='output'){const output=_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.getOutputByNumber(update.number);const oldState=output===null||output===void 0?void 0:output.isActive;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`SignalRIOUpdater: Updating output ${update.number} from ${oldState} to ${update.isActive}`);_data_IOStateData__WEBPACK_IMPORTED_MODULE_0__.IOStateData.Instance.updateOutputState(update.number,update.isActive);const outputName=this._formatIOName(output===null||output===void 0?void 0:output.name,output===null||output===void 0?void 0:output.centroidName);const displayName=outputName?`output ${update.number} (${outputName})`:`output ${update.number}`;_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.log(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`Updated ${displayName} to ${update.isActive?'ACTIVE':'INACTIVE'} via SignalR`);}}catch(error){_Logger__WEBPACK_IMPORTED_MODULE_1__.Logger.Instance.logError(_Logger__WEBPACK_IMPORTED_MODULE_1__.LogCategory.DataManager,`Error updating ${update.type} ${update.number} state:`,error);}}/**
     * Format I/O name for display. Prefers custom name, falls back to centroid name.
     * @param customName Custom user-defined name
     * @param centroidName Standard centroid name
     * @returns Formatted name or empty string if no name available
     */_formatIOName(customName,centroidName){// Prefer custom name if available
if(customName&&customName.trim()){return customName;}// Fall back to centroid name
if(centroidName&&centroidName.trim()){return centroidName;}return'';}/**
     * Get current listening status
     */get isListening(){return this._isListening;}}/**
 * Parsed I/O status update
 */_SignalRIOUpdater=SignalRIOUpdater;SignalRIOUpdater._instance=void 0;

/***/ })

}]);
//# sourceMappingURL=697.9640304c.chunk.js.map