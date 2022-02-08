import { Action, createReducer, on } from "@ngrx/store";
import { UserInfo } from "../others/Accounts";
import { setUserInfo } from "./userInfo.actions";

export const userInfo : UserInfo = new UserInfo(0,'','','','','',false,false,false,false,[]);

const _userInfoReducer = createReducer(
    userInfo,
    on(setUserInfo, (state, {userInfo}) => userInfo)
);

export function userInfoReducer(state : UserInfo | undefined, action : Action) {
    return _userInfoReducer(state, action);
}