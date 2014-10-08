using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP;

namespace AJE
{
    public class AJESolver
    {
        public double FARts0, FARps0;
        public double precooled, ts00;
        public int abflag, entype, lunits, inflag, varflag, pt2flag, wtflag;
        public int abkeep, pltkeep, move;
        public int numeng, gamopt, areaRatioScheduler, plttyp, showcom;
        public int areaThroatScheduler, areaExitScheduler, fueltype, inptype, siztype;
        // Flow variables
        public double g0d, g0, rgas, gamma, cpair;
        public double tt4, tt4d, tt7, tt7d, t8, p3p2d, p3fp2d, byprat, throtl;
        public double fsmach = 0, alt, ts0, ps0, q0, u0d, u0, a0, rho0, tsout, psout;
        public double epr, etr, npr, snpr, forceNet, forceGross, dragRam, sfc, fa, eair, uexit, ues;
        public double fnd, forceNetlb, forceGrosslb, dragRamlb, fuelFlowlb, fuelrat, forceNetTotallb, eteng;
        public double arth, areaRamThroatd, areaRamExit, areaRamExitd;
        public double machExit, pexit, pfexit;
        public double areaRamThroatMin, areaRamThroatMax, areaRamExitMin, areaRamExitMax;
        public double area8, area8rat, area8d, areaFan, a7, m2, isp;
        public double areaCap, a2, a2d, areaCore, area4, area4p, fhv, fhvd, mfr, diameng;
        public double altmin, altmax, u0min, u0max, thrmin, thrmax, pmax, tmin, tmax;
        public double etmin, etmax, cprmin, cprmax, t4min, t4max;
        public double a2min, a2max, a8min, area8max, t7min, t7max, diamin, diamax;
        public double bypmin, bypmax, fprmin, fprmax;
        public double vmn1, vmn2, vmn3, vmn4, vmx1, vmx2, vmx3, vmx4;
        public double FT2M, MPH2KPH, LBF2Newtons, PSI2kPa, F2C, tref, Lb2kg, Slug2kg, BTU2Joules, BTU2kJ;
        public double aconv, bconv, dconv, flconv;
        // weight and materials
        public double weight, wtref, wfref;
        public int mcomp, mfan, mturbin, mburner, minlt, mnozl, mnozr;
        public int ncflag, ncomp, ntflag, nturb;
        public float fireflag;
        public double dcomp, dfan, dturbin, dburner;
        public double tcomp, tfan, tturbin, tburner;
        public double tinlt, dinlt, tnozl, dnozl, tnozr, dnozr;
        public double lcomp, lburn, lturb, lnoz;   // component length
        // Station Variables
        public double[] trat = new double[20];
        public double[] tt = new double[20];
        public double[] prat = new double[20];
        public double[] pt = new double[20];
        public double[] eta = new double[20];
        public double[] gam = new double[20];
        public double[] cp = new double[20];
        public double[] s = new double[20];
        public double[] v = new double[20];
        /*				 drawing geometry  */
        public double xtrans, ytrans, factor, gains, scale;
        public double xtranp, ytranp, factp;
        public double[,] xg = new double[13, 45];
        public double[,] yg = new double[13, 45];
        public int sldloc, sldplt, ncompd;
        public int antim, ancol;
        //  Percentage  variables
        public double u0ref, altref, thrref, a2ref, et2ref, fpref, et13ref, bpref;
        public double cpref, et3ref, et4ref, et5ref, t4ref, p4ref, t7ref, et7ref, a8ref;
        public double fnref, fuelref, sfcref, airref, epref, etref, faref;
        // save design
        public int ensav, absav, gamosav, ptfsav, arssav, arthsav, arxsav, flsav;
        public double fhsav, t4sav, t7sav, p3sav, p3fsav, bysav, acsav;
        public double a2sav, a4sav, a4psav, gamsav, et2sav, pr2sav, pr4sav;
        public double et3sav, et4sav, et5sav, et7sav, et13sav, a8sav, a8mxsav;
        public double a8rtsav, u0mxsav, u0sav;
        public double trsav, artsav, arexsav;
        // save materials info
        public int wtfsav, minsav, mfnsav, mcmsav, mbrsav, mtrsav, mnlsav, mnrsav, ncsav, ntsav;
        public double wtsav, dinsav, tinsav, dfnsav, tfnsav, dcmsav, tcmsav;
        public double dbrsav, tbrsav, dtrsav, ttrsav, dnlsav, tnlsav, dnrsav, tnrsav;
        // plot variables
        public int lines, nord, nabs, param, npt, ntikx, ntiky;
        public int counter;
        public int ordkeep, abskeep;
        public double begx, endx, begy, endy;
        public double[] pltx = new double[26];
        public double[] plty = new double[26];
        public String labx, laby, labyu, labxu;

        public void comPute()
        {

            numeng = 1;
            fireflag = 0;

            getFreeStream();

            getThermo();

            getGeo(); /*						determine engine size and geometry*/

            //      view.getDrawGeo() ;

            getPerform();

            //      out.box.loadOut() ;
            //      out.vars.loadOut() ;
            //      in.fillBox() ;

            //       if (plttyp >= 3 && plttyp <= 7)  {
            //           out.plot.loadPlot () ;
            //          out.plot.repaint() ;
            //     }

            //      view.repaint() ;

            //      if (inflag == 0) myDesign() ;
        }

        public void setDefaults()
        {
            int i;

            precooled = -1;

            move = 0;
            inptype = 0;
            siztype = 0;
            lunits = 0;
            FT2M = 1.0; MPH2KPH = 1.0; LBF2Newtons = 1.0; Lb2kg = 1.0;
            PSI2kPa = 1.0; BTU2Joules = 1.0; aconv = 1.0; bconv = 1.0;
            Slug2kg = 1.0; dconv = 1.0; flconv = 1.0; BTU2kJ = 1.0;
            F2C = 1.0; tref = 459.6;
            g0 = g0d = 32.1740;

            counter = 0;
            showcom = 0;
            plttyp = 2;
            pltkeep = 0;
            entype = 0;
            inflag = 0;
            varflag = 0;
            pt2flag = 0;
            wtflag = 0;
            fireflag = 0;
            gamma = 1.4;
            gamopt = 1;
            u0d = 0.0;
            throtl = 100f;

            for (i = 0; i <= 19; ++i)
            {
                trat[i] = 1.0;
                tt[i] = 518.6;
                prat[i] = 1.0;
                pt[i] = 14.7;
                eta[i] = 1.0;
            }
            tt[4] = tt4 = tt4d = 2500f;
            tt[7] = tt7 = tt7d = 2500f;
            prat[3] = p3p2d = 8.0;
            prat[13] = p3fp2d = 2.0;
            byprat = 1.0;
            abflag = 0;

            fueltype = 0;
            fhvd = fhv = 18600f;
            a2d = a2 = areaCore = 2.0;
            diameng = Math.Sqrt(4.0 * a2d / 3.14159);
            areaCap = .9 * a2;
            area8rat = .35;
            area8 = .7;
            area8d = .40;
            areaRatioScheduler = 0;
            areaFan = 2.0;
            area4 = .418;

            areaThroatScheduler = 1;
            areaExitScheduler = 1;
            areaRamThroatMin = 0.1; areaRamThroatMax = 1.5;
            areaRamExitMin = 1.0; areaRamExitMax = 10.0;
            areaRamThroatd = arth = .4;
            areaRamExit = areaRamExitd = 3.0;

            u0min = 0.0; u0max = 1500f;
            altmin = 0.0; altmax = 60000f;
            thrmin = 30; thrmax = 100;
            etmin = .5; etmax = 1.0;
            cprmin = 1.0; cprmax = 50.0;
            bypmin = 0.0; bypmax = 10.0;
            fprmin = 1.0; fprmax = 2.0;
            t4min = 1000.0; t4max = 3200.0;
            t7min = 1000.0; t7max = 4000.0;
            a8min = 0.1; area8max = 0.4;
            a2min = .001; a2max = 50f;
            diamin = Math.Sqrt(4.0 * a2min / 3.14159);
            diamax = Math.Sqrt(4.0 * a2max / 3.14159);
            pmax = 20.0; tmin = -100.0 + tref; tmax = 100.0 + tref;
            vmn1 = u0min; vmx1 = u0max;
            vmn2 = altmin; vmx2 = altmax;
            vmn3 = thrmin; vmx3 = thrmax;
            vmn4 = areaRamExitMin; vmx4 = areaRamExitMax;

            xtrans = 125.0;
            ytrans = 115.0;
            factor = 35f;
            sldloc = 75;

            xtranp = 80.0;
            ytranp = 180.0;
            factp = 27f;
            sldplt = 138;

            weight = 1000f;
            minlt = 1; dinlt = 170.2; tinlt = 900f;
            mfan = 2; dfan = 293.02; tfan = 1500f;
            mcomp = 2; dcomp = 293.02; tcomp = 1500f;
            mburner = 4; dburner = 515.2; tburner = 2500f;
            mturbin = 4; dturbin = 515.2; tturbin = 2500f;
            mnozl = 3; dnozl = 515.2; tnozl = 2500f;
            mnozr = 5; dnozr = 515.2; tnozr = 4500f;
            ncflag = 0; ntflag = 0;



            FT2M = .3048; MPH2KPH = 1.60934; LBF2Newtons = 4.44822162;
            BTU2Joules = 1055f; BTU2kJ = 1.055;
            Lb2kg = .453592; PSI2kPa = 6.89475729; F2C = 0.555555;
            Slug2kg = 14.5939029; tref = 273.15;
            return;
        }

        public void loadCF6()
        {

            entype = 2;
            abflag = 0;
            fueltype = 0;
            fhvd = fhv = 18600f;
            tt[4] = tt4 = tt4d = 2500f;
            tt[7] = tt7 = tt7d = 2500f;
            prat[3] = p3p2d = 21.86;
            prat[13] = p3fp2d = 1.745;
            byprat = 4.0;
            areaCore = 6.965;
            areaFan = areaCore * (1.0 + byprat);
            a2d = a2 = areaFan;
            diameng = Math.Sqrt(4.0 * a2d / 3.14159);
            area4 = .290;
            area4p = 1.131;
            areaCap = .9 * a2;
            gamma = 1.4;
            gamopt = 1;
            pt2flag = 0;
            eta[2] = 1.0;
            prat[2] = 1.0;
            prat[4] = 1.0;
            eta[3] = .959;
            eta[4] = .984;
            eta[5] = .982;
            eta[7] = 1.0;
            eta[13] = 1.0;
            area8d = 2.436;
            area8max = .35;
            area8rat = .35;

            u0max = 1500f;
            u0d = 0.0;
            areaRatioScheduler = 0;

            wtflag = 0; weight = 8229f;
            minlt = 1; dinlt = 170f; tinlt = 900f;
            mfan = 2; dfan = 293f; tfan = 1500f;
            mcomp = 0; dcomp = 293f; tcomp = 1600f;
            mburner = 4; dburner = 515f; tburner = 2500f;
            mturbin = 4; dturbin = 515f; tturbin = 2500f;
            mnozl = 3; dnozl = 515f; tnozl = 2500f;
            ncflag = 0; ntflag = 0;

            //      con.setPanl() ;
            return;
        }

        public void loadJ85()
        {

            entype = 0;
            abflag = 0;
            fueltype = 0;
            fhvd = fhv = 18600f;
            tt[4] = tt4 = tt4d = 2260f;
            tt[7] = tt7 = tt7d = 4000f;
            prat[3] = p3p2d = 8.3;
            prat[13] = p3fp2d = 1.0;
            byprat = 0.0;
            a2d = a2 = areaCore = 1.753;
            diameng = Math.Sqrt(4.0 * a2d / 3.14159);
            areaFan = areaCore * (1.0 + byprat);
            area4 = .323;
            area4p = .818;
            areaCap = .9 * a2;
            gamma = 1.4;
            gamopt = 1;
            pt2flag = 0;
            eta[2] = 1.0;
            prat[2] = 1.0;
            prat[4] = .85;
            eta[3] = .822;
            eta[4] = .982;
            eta[5] = .882;
            eta[7] = .97;
            eta[13] = 1.0;
            area8d = .818;
            area8max = .467;
            area8rat = .467;

            u0max = 1500f;
            u0d = 0.0;
            areaRatioScheduler = 1;

            wtflag = 0; weight = 561f;
            minlt = 1; dinlt = 170f; tinlt = 900f;
            mfan = 2; dfan = 293f; tfan = 1500f;
            mcomp = 2; dcomp = 293f; tcomp = 1500f;
            mburner = 4; dburner = 515f; tburner = 2500f;
            mturbin = 4; dturbin = 515f; tturbin = 2500f;
            mnozl = 5; dnozl = 600f; tnozl = 4100f;
            ncflag = 0; ntflag = 0;

            //     con.setPanl() ;
            return;
        }

        public void loadF100()
        {

            entype = 1;
            abflag = 1;
            fueltype = 0;
            fhvd = fhv = 18600f;
            tt[4] = tt4 = tt4d = 2499f;
            tt[7] = tt7 = tt7d = 3905f;
            prat[3] = p3p2d = 20.04;
            prat[13] = p3fp2d = 1.745;
            byprat = 0.0;
            a2d = a2 = areaCore = 6.00;
            diameng = Math.Sqrt(4.0 * a2d / 3.14159);
            areaFan = areaCore * (1.0 + byprat);
            area4 = .472;
            area4p = 1.524;
            areaCap = .9 * a2;
            gamma = 1.4;
            gamopt = 1;
            pt2flag = 0;
            eta[2] = 1.0;
            prat[2] = 1.0;
            prat[4] = 1.0;
            eta[3] = .959;
            eta[4] = .984;
            eta[5] = .982;
            eta[7] = .92;
            eta[13] = 1.0;
            area8d = 1.524;
            area8max = .335;
            area8rat = .335;

            u0max = 1500f;
            u0d = 0.0;
            areaRatioScheduler = 0;

            wtflag = 0; weight = 3875f;
            minlt = 1; dinlt = 170f; tinlt = 900f;
            mfan = 2; dfan = 293f; tfan = 1500f;
            mcomp = 2; dcomp = 293f; tcomp = 1700f;
            mburner = 4; dburner = 515f; tburner = 2500f;
            mturbin = 4; dturbin = 515f; tturbin = 2500f;
            mnozl = 5; dnozl = 400.2; tnozl = 4100f;
            ncflag = 0; ntflag = 0;

            //       con.setPanl() ;
            return;
        }

        public void loadRamj()
        {

            entype = 3;
            areaThroatScheduler = 0;
            areaExitScheduler = 0;
            areaRamThroatd = .4;
            areaRamExitd = 3.0;
            abflag = 0;
            fueltype = 0;
            fhvd = fhv = 18600f;
            tt[4] = tt4 = tt4d = 4000f;
            t4max = 4500f;
            tt[7] = tt7 = tt7d = 4000f;
            prat[3] = p3p2d = 1.0;
            prat[13] = p3fp2d = 1.0;
            byprat = 0.0;
            a2d = a2 = areaCore = 1.753;
            diameng = Math.Sqrt(4.0 * a2d / 3.14159);
            areaFan = areaCore * (1.0 + byprat);
            area4 = .323;
            area4p = .818;
            areaCap = .9 * a2;
            gamma = 1.4;
            gamopt = 1;
            pt2flag = 0;
            eta[2] = 1.0;
            prat[2] = 1.0;
            prat[4] = 1.0;
            eta[3] = 1.0;
            eta[4] = .982;
            eta[5] = 1.0;
            eta[7] = 1.0;
            eta[13] = 1.0;
            area8 = area8d = 2.00;
            area8max = 15f;
            area8rat = 4.0;
            a7 = .50;

            u0max = 4500f;
            u0d = 2200.0;
            areaRatioScheduler = 0;

            wtflag = 0; weight = 976f;
            minlt = 2; dinlt = 293f; tinlt = 4000;
            mfan = 2; dfan = 293f; tfan = 1500f;
            mcomp = 2; dcomp = 293f; tcomp = 1500f;
            mburner = 7; dburner = 515f; tburner = 4500f;
            mturbin = 4; dturbin = 515f; tturbin = 2500f;
            mnozr = 5; dnozr = 515.2; tnozr = 4500f;
            ncflag = 0; ntflag = 0;

            //      con.setPanl() ;
            return;
        }

        public void getFreeStream()
        {
            rgas = 1718f;                /*						 ft2/sec2 R */
            /*        if (inptype >= 2)
                    {
                        ps0 = ps0 * 144f;
                    }
                    if (inptype <= 1)
                    {            /*						 input altitude */
            /*              alt = altd / lconv1;
                          if (alt < 36152f)
                          {
                              ts0 = 518.6 - 3.56 * alt / 1000f;
                              ps0 = 2116f * Math.Pow(ts0 / 518.6, 5.256);
                          }
                          if (alt >= 36152f && alt <= 82345f)
                          {   // Stratosphere
                              ts0 = 389.98;
                              ps0 = 2116f * .2236 *
                                    Math.Exp((36000f - alt) / (53.35 * 389.98));
                          }
                          if (alt >= 82345f)
                          {
                              ts0 = 389.98 + 1.645 * (alt - 82345) / 1000f;
                              ps0 = 2116f * .02456 * Math.Pow(ts0 / 389.98, -11.388);
                          }
                          ts00 = ts0;
                          if (precooled != -1)
                          {
                              tt[0] = precooled;
                          }

                      } */

            ts0 = FARts0 * 1.8; // kelvin to rankine
            ps0 = FARps0 * 14.696 * 144; // atm to psi to lb/sqft


            a0 = Math.Sqrt(gamma * rgas * ts0);             /*						 speed of sound ft/sec */



            if (inptype == 0 || inptype == 2)
            {           /*						 input speed  */
                u0 = u0d / MPH2KPH * 5280f / 3600f;           /*								 airspeed ft/sec */
                fsmach = u0 / a0;
                q0 = gamma / 2.0 * fsmach * fsmach * ps0;
            }
            if (inptype == 1 || inptype == 3)
            {            /*						 input mach */
                u0 = fsmach * a0;
                u0d = u0 * MPH2KPH / 5280f * 3600f;      /*								 airspeed ft/sec */
                q0 = gamma / 2.0 * fsmach * fsmach * ps0;
            }
            if (u0 > .0001) rho0 = q0 / (u0 * u0);
            else rho0 = 1.0;

            if (precooled != -1)
                tt[0] = precooled;
            else
                tt[0] = ts0 * (1.0 + .5 * (gamma - 1.0) * fsmach * fsmach);



            pt[0] = ps0 * Math.Pow(tt[0] / ts0, gamma / (gamma - 1.0));
            ps0 = ps0 / 144f;
            pt[0] = pt[0] / 144f;
            cpair = getCp(tt[0], gamopt);              /*						BTU/lbm R */
            tsout = ts0;
            psout = ps0;



            return;
        }

        public void getThermo()
        {
            double m5;
            double delhc, delhht, delhf, delhlt;
            double deltc, deltht, deltf, deltlt;
            
            prat[2] = eta[2]; // assume the inlet already took care of this
            // so just use the entered value.

            /*						 protection for overwriting input */
            // don't clamp values.
            /*if (eta[3] < .5) eta[3] = .5;
            if (eta[5] < .5) eta[5] = .5;*/
            trat[7] = 1.0;
            prat[7] = 1.0;
            tt[2] = tt[1] = tt[0];



            pt[1] = pt[0];
            gam[2] = getGamma(tt[2], gamopt);
            cp[2] = getCp(tt[2], gamopt);
            pt[2] = pt[1] * prat[2];
            /*						 design - p3p2 specified - tt4 specified */
            if (entype <= 1)
            {              /*								 turbojet */
                prat[3] = p3p2d;                      /*										 core compressor */
                if (prat[3] < .5) prat[3] = .5;
                delhc = (cp[2] * tt[2] / eta[3]) *
                        (Math.Pow(prat[3], (gam[2] - 1.0) / gam[2]) - 1.0);
                deltc = delhc / cp[2];
                pt[3] = pt[2] * prat[3];
                tt[3] = tt[2] + deltc;
                trat[3] = tt[3] / tt[2];
                gam[3] = getGamma(tt[3], gamopt);
                cp[3] = getCp(tt[3], gamopt);
                tt[4] = tt4 * throtl / 100.0;
                gam[4] = getGamma(tt[4], gamopt);
                cp[4] = getCp(tt[4], gamopt);
                trat[4] = tt[4] / tt[3];
                pt[4] = pt[3] * prat[4];
                delhht = delhc;
                deltht = delhht / cp[4];
                tt[5] = tt[4] - deltht;
                gam[5] = getGamma(tt[5], gamopt);
                cp[5] = getCp(tt[5], gamopt);
                trat[5] = tt[5] / tt[4];
                prat[5] = Math.Pow((1.0 - delhht / cp[4] / tt[4] / eta[5]),
                    (gam[4] / (gam[4] - 1.0)));
                pt[5] = pt[4] * prat[5];
                /*										 fan conditions */
                prat[13] = 1.0;
                trat[13] = 1.0;
                tt[13] = tt[2];
                pt[13] = pt[2];
                gam[13] = gam[2];
                cp[13] = cp[2];
                prat[15] = 1.0;
                pt[15] = pt[5];
                trat[15] = 1.0;
                tt[15] = tt[5];
                gam[15] = gam[5];
                cp[15] = cp[5];
            }

            if (entype == 2)
            {                         /*								 turbofan */
                prat[13] = p3fp2d;
                if (prat[13] < .5) prat[13] = .5;
                delhf = (cp[2] * tt[2] / eta[13]) *
                        (Math.Pow(prat[13], (gam[2] - 1.0) / gam[2]) - 1.0);
                deltf = delhf / cp[2];
                tt[13] = tt[2] + deltf;
                pt[13] = pt[2] * prat[13];
                trat[13] = tt[13] / tt[2];
                gam[13] = getGamma(tt[13], gamopt);
                cp[13] = getCp(tt[13], gamopt);
                prat[3] = p3p2d;                      /*										 core compressor */
                if (prat[3] < .5) prat[3] = .5;
                delhc = (cp[13] * tt[13] / eta[3]) *
                        (Math.Pow(prat[3], (gam[13] - 1.0) / gam[13]) - 1.0);
                deltc = delhc / cp[13];
                tt[3] = tt[13] + deltc;
                pt[3] = pt[13] * prat[3];
                trat[3] = tt[3] / tt[13];
                gam[3] = getGamma(tt[3], gamopt);
                cp[3] = getCp(tt[3], gamopt);
                tt[4] = tt4 * throtl / 100.0;
                pt[4] = pt[3] * prat[4];
                gam[4] = getGamma(tt[4], gamopt);
                cp[4] = getCp(tt[4], gamopt);
                trat[4] = tt[4] / tt[3];
                delhht = delhc;
                deltht = delhht / cp[4];
                tt[5] = tt[4] - deltht;
                gam[5] = getGamma(tt[5], gamopt);
                cp[5] = getCp(tt[5], gamopt);
                trat[5] = tt[5] / tt[4];
                prat[5] = Math.Pow((1.0 - delhht / cp[4] / tt[4] / eta[5]),
                    (gam[4] / (gam[4] - 1.0)));
                pt[5] = pt[4] * prat[5];
                delhlt = (1.0 + byprat) * delhf;
                deltlt = delhlt / cp[5];
                tt[15] = tt[5] - deltlt;
                gam[15] = getGamma(tt[15], gamopt);
                cp[15] = getCp(tt[15], gamopt);
                trat[15] = tt[15] / tt[5];
                prat[15] = Math.Pow((1.0 - delhlt / cp[5] / tt[5] / eta[5]),
                    (gam[5] / (gam[5] - 1.0)));
                pt[15] = pt[5] * prat[15];
            }

            if (entype == 3)
            {              /*								 ramjet */
                prat[3] = 1.0;
                pt[3] = pt[2] * prat[3];
                tt[3] = tt[2];
                trat[3] = 1.0;
                gam[3] = getGamma(tt[3], gamopt);
                cp[3] = getCp(tt[3], gamopt);
                tt[4] = tt4 * throtl / 100.0;
                gam[4] = getGamma(tt[4], gamopt);
                cp[4] = getCp(tt[4], gamopt);
                trat[4] = tt[4] / tt[3];
                pt[4] = pt[3] * prat[4];
                tt[5] = tt[4];
                gam[5] = getGamma(tt[5], gamopt);
                cp[5] = getCp(tt[5], gamopt);
                trat[5] = 1.0;
                prat[5] = 1.0;
                pt[5] = pt[4];
                /*										 fan conditions */
                prat[13] = 1.0;
                trat[13] = 1.0;
                tt[13] = tt[2];
                pt[13] = pt[2];
                gam[13] = gam[2];
                cp[13] = cp[2];
                prat[15] = 1.0;
                pt[15] = pt[5];
                trat[15] = 1.0;
                tt[15] = tt[5];
                gam[15] = gam[5];
                cp[15] = cp[5];
            }

            tt[7] = tt7;
            /*						 analysis -assume flow choked at both turbine entrances */
            /* and nozzle throat ... then*/

            prat[6] = 1.0;
            pt[6] = pt[15];
            trat[6] = 1.0;
            tt[6] = tt[15];
            gam[6] = getGamma(tt[6], gamopt);
            cp[6] = getCp(tt[6], gamopt);
            if (abflag > 0)
            {                   /*						 afterburner */
                trat[7] = tt[7] / tt[6];
                m5 = getMach(0, getAir(1.0, gam[5]) * area4 / areaCore, gam[5]);
                prat[7] = getRayleighLoss(m5, trat[7], tt[6]);
            }
            tt[7] = tt[6] * trat[7];
            pt[7] = pt[6] * prat[7];
            gam[7] = getGamma(tt[7], gamopt);
            cp[7] = getCp(tt[7], gamopt);
            /*						 engine press ratio EPR*/
            epr = prat[7] * prat[15] * prat[5] * prat[4] * prat[3] * prat[13];
            /*						 engine temp ratio ETR */
            etr = trat[7] * trat[15] * trat[5] * trat[4] * trat[3] * trat[13];
            return;
        }

        public void getPerform()
        {       /*				 determine engine performance */
            double fac1, game, cpe, cp3, rg, p8p5, rg1;
            int index;

            rg1 = 53.3;
            rg = cpair * (gamma - 1.0) / gamma;
            cp3 = getCp(tt[3], gamopt);                  /*						BTU/lbm R */
            g0 = 32.1740;
            ues = 0.0;
            game = getGamma(tt[5], gamopt);
            fac1 = (game - 1.0) / game;
            cpe = getCp(tt[5], gamopt);
            // remove minimums for eta7 and eta4
            //if (eta[7] < .8) eta[7] = .8;    /*						 protection during overwriting */
            //if (eta[4] < .8) eta[4] = .8;

            /*						  specific net thrust  - thrust / (g0*airflow) -   lbf/lbm/sec  */
            // turbine engine core
            if (entype <= 2)
            {
                /*								 airflow determined at choked nozzle exit */
                pt[8] = epr * prat[2] * pt[0];
                eair = getAir(1.0, game) * 144f * area8 * pt[8] / 14.7 /
                       Math.Sqrt(etr * tt[0] / 518f);
                m2 = getMach(0, eair * Math.Sqrt(tt[0] / 518f) /
                    (prat[2] * pt[0] / 14.7 * areaCore * 144f), gamma);
                npr = pt[8] / ps0;
                uexit = Math.Sqrt(2.0 * rgas / fac1 * etr * tt[0] * eta[7] *
                    (1.0 - Math.Pow(1.0 / npr, fac1)));
                if (npr <= 1.893) pexit = ps0;
                else pexit = .52828 * pt[8];
                forceGross = (uexit + (pexit - ps0) * 144f * area8 / eair) / g0;
            }

            // turbo fan -- added terms for fan flow
            if (entype == 2)
            {
                fac1 = (gamma - 1.0) / gamma;
                snpr = pt[13] / ps0;
                ues = Math.Sqrt(2.0 * rgas / fac1 * tt[13] * eta[7] *
                    (1.0 - Math.Pow(1.0 / snpr, fac1)));
                m2 = getMach(0, eair * (1.0 + byprat) * Math.Sqrt(tt[0] / 518f) /
                    (prat[2] * pt[0] / 14.7 * areaFan * 144f), gamma);
                if (snpr <= 1.893) pfexit = ps0;
                else pfexit = .52828 * pt[13];
                forceGross = forceGross + (byprat * ues + (pfexit - ps0) * 144f * byprat * areaCore / eair) / g0;
            }

            // ramjets
            if (entype == 3)
            {
                /*								 airflow determined at nozzle throat */
                eair = getAir(1.0, game) * 144.0 * a2 * areaRamThroatd * epr * prat[2] * pt[0] / 14.7 /
                       Math.Sqrt(etr * tt[0] / 518f);
                m2 = getMach(0, eair * Math.Sqrt(tt[0] / 518f) /
                    (prat[2] * pt[0] / 14.7 * areaCore * 144f), gamma);
                machExit = getMach(2, (getAir(1.0, game) / areaRamExitd), game);
                uexit = machExit * Math.Sqrt(game * rgas * etr * tt[0] * eta[7] /
                    (1.0 + .5 * (game - 1.0) * machExit * machExit));
                pexit = Math.Pow((1.0 + .5 * (game - 1.0) * machExit * machExit), (-game / (game - 1.0)))
                        * epr * prat[2] * pt[0];
                forceGross = (uexit + (pexit - ps0) * areaRamExitd * areaRamThroatd * a2 / eair / 144f) / g0;
            }

            // ram drag
            dragRam = u0 / g0;
            if (entype == 2) dragRam = dragRam + u0 * byprat / g0;

            // mass flow ratio 
            if (fsmach > .01) mfr = getAir(m2, gamma) * prat[2] / getAir(fsmach, gamma);
            else mfr = 5f;

            // net thrust
            forceNet = forceGross - dragRam;
            if (entype == 3 && fsmach < .3)
            {
                forceNet = 0.0;
                forceGross = 0.0;
            }

            // thrust in pounds
            forceNetlb = forceNet * eair;
            forceGrosslb = forceGross * eair;
            dragRamlb = dragRam * eair;

            //fuel-air ratio and sfc
            fa = (trat[4] - 1.0) / (eta[4] * fhv / (cp3 * tt[3]) - trat[4]) +
                 (trat[7] - 1.0) / (fhv / (cpe * tt[15]) - trat[7]);
            if (forceNet > 0.0)
            {
                sfc = 3600f * fa / forceNet;
                fuelFlowlb = sfc * forceNetlb;
                isp = (forceNetlb / fuelFlowlb) * 3600f;
            }
            else
            {
                forceNetlb = 0.0;
                fuelFlowlb = 0.0;
                sfc = 0.0;
                isp = 0.0;
            }

            // plot output
            tt[8] = tt[7];
            t8 = etr * tt[0] - uexit * uexit / (2.0 * rgas * game / (game - 1.0));
            trat[8] = tt[8] / tt[7];
            p8p5 = ps0 / (epr * prat[2] * pt[0]);
            cp[8] = getCp(tt[8], gamopt);
            pt[8] = pt[7];
            prat[8] = pt[8] / pt[7];
            /*						 thermal efficiency */
            if (entype == 2)
            {
                eteng = (a0 * a0 * ((1.0 + fa) * (uexit * uexit / (a0 * a0))
                    + byprat * (ues * ues / (a0 * a0))
                    - (1.0 + byprat) * fsmach * fsmach)) / (2.0 * g0 * fa * fhv * 778.16);
            }
            else
            {
                eteng = (a0 * a0 * ((1.0 + fa) * (uexit * uexit / (a0 * a0))
                    - fsmach * fsmach)) / (2.0 * g0 * fa * fhv * 778.16);
            }

            s[0] = s[1] = .2;
            v[0] = v[1] = rg1 * ts0 / (ps0 * 144f);
            for (index = 2; index <= 7; ++index)
            {     /*						 compute entropy */
                s[index] = s[index - 1] + cpair * Math.Log(trat[index])
                           - rg * Math.Log(prat[index]);
                v[index] = rg1 * tt[index] / (pt[index] * 144f);
            }
            s[13] = s[2] + cpair * Math.Log(trat[13]) - rg * Math.Log(prat[13]);
            v[13] = rg1 * tt[13] / (pt[13] * 144f);
            s[15] = s[5] + cpair * Math.Log(trat[15]) - rg * Math.Log(prat[15]);
            v[15] = rg1 * tt[15] / (pt[15] * 144f);
            s[8] = s[7] + cpair * Math.Log(t8 / (etr * tt[0])) - rg * Math.Log(p8p5);
            v[8] = rg1 * t8 / (ps0 * 144f);
            cp[0] = getCp(tt[0], gamopt);

            forceNetTotallb = numeng * forceNetlb;
            fuelrat = numeng * fuelFlowlb;
            // weight  calculation
            /*if (wtflag == 0)
            {
                if (entype == 0)
                {
                    weight = .132 * Math.Sqrt(acore * acore * acore) *
                             (dcomp * lcomp + dburner * lburn + dturbin * lturb + dnozl * lnoz);
                }
                if (entype == 1)
                {
                    weight = .100 * Math.Sqrt(acore * acore * acore) *
                             (dcomp * lcomp + dburner * lburn + dturbin * lturb + dnozl * lnoz);
                }
                if (entype == 2)
                {
                    weight = .0932 * acore * ((1.0 + byprat) * dfan * 4.0 + dcomp * (ncomp - 3) +
                        dburner + dturbin * nturb + dnozl * 2.0) * Math.Sqrt(acore / 6.965);
                }
                if (entype == 3)
                {
                    weight = .1242 * acore * (dburner + dnozr * 6f + dinlt * 3f) * Math.Sqrt(acore / 1.753);
                }
            }*/
            // check for temp limits
            if (entype < 3)
            {
                //    if (tt[2] > tinlt) {
                fireflag = (float)Math.Max(fireflag, (float)ts00 * (1.0 + .5 * (gamma - 1.0) * fsmach * fsmach) / tinlt);
                /*								        out.vars.to1.setForeground(Color.red) ;
             out.vars.to2.setForeground(Color.red) ; */
                //    }
                //     if (tt[13] > tfan) {
                fireflag = (float)Math.Max(fireflag, (float)tt[13] / tfan);
                //        out.vars.to2.setForeground(Color.red) ;
                //     }
                //     if (tt[3] > tcomp) {
                fireflag = (float)Math.Max(fireflag, (float)tt[3] / tcomp);
                //       out.vars.to3.setForeground(Color.red) ;
                //    }
                //    if (tt[4] > tburner) {
                //       fireflag = (float)Math.Max (fireflag,(float)tt[4]/tburner) ;      
                //       out.vars.to4.setForeground(Color.red) ;
                //   }
                //   if (tt[5] > tturbin) {
                //      fireflag = (float)Math.Max (fireflag,(float)tt[5]/tturbin) ;      
                //       out.vars.to5.setForeground(Color.red) ;
                //    }
                //   if (tt[7] > tnozl) {
                //      fireflag = (float)Math.Max (fireflag,(float)tt[7]/tnozl) ;      
                //       out.vars.to6.setForeground(Color.red) ;
                //       out.vars.to7.setForeground(Color.red) ;
                //    }
            }
            if (entype == 3)
            {
                fireflag = (float)Math.Max(fireflag, (float)tt[3] / tinlt);

                /*									       out.vars.to1.setForeground(Color.red) ;
             out.vars.to2.setForeground(Color.red) ;
             out.vars.to3.setForeground(Color.red) ; */
                fireflag = (float)Math.Max(fireflag, (float)tt[4] / tburner);

                //       out.vars.to4.setForeground(Color.red) ;

                fireflag = (float)Math.Max(fireflag, (float)tt[7] / tnozr);

                /*									       out.vars.to5.setForeground(Color.red) ;
             out.vars.to6.setForeground(Color.red) ;
             out.vars.to7.setForeground(Color.red) ;*/
            }

            //if (fireflag == 1) view.start() ;
        }

        public void getGeo()
        {
            /*						 determine geometric variables */
            double gammaExit;

            if (entype <= 2)
            {          // turbine engines
                if (areaFan < areaCore) areaFan = areaCore;
                area8max = .75 * Math.Sqrt(etr) / epr; /*								 limits compressor face  */
                /*  mach number  to < .5   */
                if (area8max > 1.0) area8max = 1.0;
                if (area8rat > area8max)
                {
                    area8rat = area8max;
                }
                /*								    dumb down limit - a8 schedule */
                if (areaRatioScheduler == 0)
                {
                    area8rat = area8max;
                }
                area8 = area8rat * areaCore;
                area8d = area8 * prat[7] / Math.Sqrt(trat[7]);
                /*								 assumes choked a8 and a4 */
                area4 = area8 * prat[5] * prat[15] * prat[7] /
                     Math.Sqrt(trat[7] * trat[5] * trat[15]);
                area4p = area8 * prat[15] * prat[7] / Math.Sqrt(trat[7] * trat[15]);
                areaCap = .9 * a2;
            }

            if (entype == 3)
            {      // ramjets
                gammaExit = getGamma(tt[4], gamopt);
                if (areaThroatScheduler == 0)
                {   // scheduled throat area
                    areaRamThroatd = getAir(fsmach, gamma) * Math.Sqrt(etr) /
                            (getAir(1.0, gammaExit) * epr * prat[2]);
                    if (areaRamThroatd < areaRamThroatMin) areaRamThroatd = areaRamThroatMin;
                    if (areaRamThroatd > areaRamThroatMax) areaRamThroatd = areaRamThroatMax;
                }
                if (areaExitScheduler == 0)
                {   // scheduled exit area
                    machExit = Math.Sqrt((2.0 / (gammaExit - 1.0)) * ((1.0 + .5 * (gamma - 1.0) * fsmach * fsmach)
                        * Math.Pow((epr * prat[2]), (gammaExit - 1.0) / gammaExit) - 1.0));
                    areaRamExitd = getAir(1.0, gammaExit) / getAir(machExit, gammaExit);
                    if (areaRamExitd < areaRamExitMin) areaRamExitd = areaRamExitMin;
                    if (areaRamExitd > areaRamExitMax) areaRamExitd = areaRamExitMax;
                }
            }
        }

        public int filter0(double inumbr)
        {
            //  output only to .
            float number;
            int intermed;

            intermed = (int)(inumbr);
            number = (float)(intermed);
            return intermed;
        }

        public float filter1(double inumbr)
        {
            //  output only to .1
            float number;
            int intermed;

            intermed = (int)(inumbr * 10f);
            number = (float)(intermed / 10f);
            return number;
        }

        public float filter3(double inumbr)
        {
            //  output only to .001
            float number;
            int intermed;

            intermed = (int)(inumbr * 1000f);
            number = (float)(intermed / 1000f);
            return number;
        }

        public double getGamma(double temp, int opt)
        {
            // Utility to get gamma as a function of temp 
            double number, a, b, c, d;
            a = -7.6942651e-13;
            b = 1.3764661e-08;
            c = -7.8185709e-05;
            d = 1.436914;
            if (opt == 0)
            {
                number = 1.4;
            }
            else
            {
                number = a * temp * temp * temp + b * temp * temp + c * temp + d;
            }
            return (number);
        }

        // specific heat of air at constant pressure, as defined by temperature.
        public double getCp(double temp, int opt)
        {
            // Utility to get cp as a function of temp 
            double number, a, b, c, d;
            /*						 BTU/R */
            a = -4.4702130e-13;
            b = -5.1286514e-10;
            c = 2.8323331e-05;
            d = 0.2245283;
            if (opt == 0)
            {
                number = .2399;
            }
            else
            {
                number = a * temp * temp * temp + b * temp * temp + c * temp + d;
            }
            return (number);
        }

        public double getMach(int sub, double corair, double gamma)
        {
            /*						 Utility to get the Mach number given the corrected airflow per area */
            double number, chokair;              /*						 iterate for mach number */
            double deriv, machn, macho, airo, airn;
            int iter;

            chokair = getAir(1.0, gamma);
            if (corair > chokair)
            {
                number = 1.0;
                return (number);
            }
            else
            {
                airo = .25618;                 /*								 initial guess */
                if (sub == 1) macho = 1.0;   /*								 sonic */
                else
                {
                    if (sub == 2) macho = 1.703; /*										 supersonic */
                    else macho = .5;                /*										 subsonic */
                    iter = 1;
                    machn = macho - .2;
                    while (Math.Abs(corair - airo) > .0001 && iter < 20)
                    {
                        airn = getAir(machn, gamma);
                        deriv = (airn - airo) / (machn - macho);
                        airo = airn;
                        macho = machn;
                        machn = macho + (corair - airo) / deriv;
                        ++iter;
                    }
                }
                number = macho;
            }
            return (number);
        }

        public double getRayleighLoss(double mach1, double ttrat, double tlow)
        {
            /*						 analysis for rayleigh flow */
            double number;
            double wc1, wc2, mgueso, mach2, g1, gm1, g2, gm2;
            double fac1, fac2, fac3, fac4;

            g1 = getGamma(tlow, gamopt);
            gm1 = g1 - 1.0;
            wc1 = getAir(mach1, g1);
            g2 = getGamma(tlow * ttrat, gamopt);
            gm2 = g2 - 1.0;
            number = .95;
            /*						 iterate for mach downstream */
            mgueso = .4;                 /*						 initial guess */
            mach2 = .5;

            int itcount = 0;
            while (Math.Abs(mach2 - mgueso) > .0001 && itcount < 200)
            {
                itcount++;
                mgueso = mach2;
                fac1 = 1.0 + g1 * mach1 * mach1;
                fac2 = 1.0 + g2 * mach2 * mach2;
                fac3 = Math.Pow((1.0 + .5 * gm1 * mach1 * mach1), (g1 / gm1));
                fac4 = Math.Pow((1.0 + .5 * gm2 * mach2 * mach2), (g2 / gm2));
                number = fac1 * fac4 / fac2 / fac3;
                wc2 = wc1 * Math.Sqrt(ttrat) / number;
                mach2 = getMach(0, wc2, g2);
            }
            return (number);
        }

        public double getAir(double mach, double gamma)
        {
            /*						 Utility to get the corrected airflow per area given the Mach number */
            double number, fac1, fac2;
            fac2 = (gamma + 1.0) / (2.0 * (gamma - 1.0));
            fac1 = Math.Pow((1.0 + .5 * (gamma - 1.0) * mach * mach), fac2);
            number = .50161 * Math.Sqrt(gamma) * mach / fac1;

            return (number);
        }


    }    // end Solver






}

