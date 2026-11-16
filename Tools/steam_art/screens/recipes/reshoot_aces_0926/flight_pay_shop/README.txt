ACES re-capture 2026-09-26 of slots 02 (14_handover_flight), 04 (10_payslip), 08 (11_upgrades_shop).
Copies of reshoot_0926/reshoot_ho/{a_pre,d1_take,d2_setup,d3_fly,e2_setup,e3_pay,g_restore} and reshoot_d/s11_a..d.
Only change: the Editor stays PAUSED between evals (a_pre/g_restore start with isPaused=true, the trailing unpause of
d1_take/d2_setup/e2_setup is now a pause, d3_fly/e3_pay re-pause after their last action). In-eval order is unchanged.
Order: a_pre d2_setup d3_fly | d1_take e2_setup e3_pay e4b_shot (e4_shot = first take: floor-strip pill mid-fade + OS-mouse head, discarded) | d1_take s11_a s11_b s11_c s11_d (final) | g_restore
