# HideoutSiege

将原版藏身处攻击改造成“讨敌骂阵 → 强盗集结 → 破城寨攻坚”的原型 Mod。

## 当前规则

- 藏身处入口新增 **讨敌骂阵**。
- 点击后立刻进入时间快进。
- 战斗时刻不是固定 +24 小时，而是 **至少完整经过 24 小时，再到随后第一个正午 12:00**，即 24+n。
- 在半径 `60` 个地图单位内、与目标藏身处同 Culture 的地图强盗队伍会被锁定并真实向藏身处移动。
- 等待期间可以选择 **结束攻城**。这是取消整个攻城流程，不是暂停；取消后玩家退出藏身处遭遇，被召回强盗恢复自主 AI。
- 到期后仍在路上的可调度强盗会通过原版 `EnterSettlementAction` 进入藏身处，然后才创建 Hideout MapEvent。
- 不打开部队选择界面；玩家主部队与藏身处全部守军进入同一个 MapEvent，实际出生数量继续受原版 Battle Size / reinforcement 规则控制。
- 战斗场景从巴丹尼亚城镇的 `center` 场景中随机复用。
- 使用原版 `OpenSiegeMissionNoDeployment`，但仅在 HideoutSiege 战斗中将 `MissionCombatantsLogic` 的 AI 类型改为 `Siege`，并屏蔽城镇攻城专用的守军撤退阈值。随后注入 `HideoutSiegePreparationLogic`：破坏最多两个可破坏城墙段、禁用攻城器与梯子。
- HideoutSiege 战斗期间屏蔽通用 nearby-party 自动加入，避免冻结后的参战名单被额外 NPC 污染。

## 首版可调常量

见 `HideoutSiege/Config/HideoutSiegeConfig.cs`：

- `RallyRadiusMapUnits = 60`
- `MinimumRallyHours = 24`
- `BattleStartHour = 12`
- `BattanianCultureId = "battania"`

## 仍需游戏内验证

1. 巴丹尼亚各城镇场景在 `OpenSiegeMissionNoDeployment` 下的 `battle_set`、出生点和 AI 路径。
2. 两个 `WallSegment` 的 broken-child 缺口是否在所有候选图都可通行。
3. Hideout MapEvent + Siege Mission 的撤退、失败、胜利结算是否完整回到原版藏身处结算链。
4. 保存/加载发生在集结等待期间时，原版 WaitMenu 是否能稳定恢复快进状态。
5. 被外部战斗占用的强盗不会被强行从 MapEvent 中拔出；这类单位当前不会被本次“讨敌骂阵”纳入可调度列表。
